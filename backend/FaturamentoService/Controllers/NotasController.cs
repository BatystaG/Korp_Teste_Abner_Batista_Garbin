using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FaturamentoService.Data;
using FaturamentoService.Models;

namespace FaturamentoService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotasController : ControllerBase
{
    private readonly FaturamentoDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    // HttpClient é injetado via IHttpClientFactory — melhor prática para evitar
    // esgotamento de sockets ao reutilizar conexões
    public NotasController(FaturamentoDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpClient = httpFactory.CreateClient("EstoqueService");
        _config = config;
    }

    // GET /api/notas
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notas = await _db.NotasFiscais.Include(n => n.Itens).ToListAsync();
        return Ok(notas);
    }

    // GET /api/notas/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var nota = await _db.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id);
        if (nota is null) return NotFound();
        return Ok(nota);
    }

    // POST /api/notas
    [HttpPost]
    public async Task<IActionResult> Create(NotaFiscal nota)
    {
        nota.Status = "Rascunho";
        _db.NotasFiscais.Add(nota);
        await _db.SaveChangesAsync();

        // Numeração sequencial gerada após o Id ser atribuído pelo banco
        nota.Numero = nota.Id.ToString("D5");
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nota.Id }, nota);
    }

    // PUT /api/notas/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, NotaFiscal nota)
    {
        var existing = await _db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (existing is null) return NotFound();
        if (existing.Status == "Impressa")
            return BadRequest(new { erro = "Nota já impressa não pode ser alterada." });

        // Remove itens antigos e substitui pelos novos
        _db.RemoveRange(existing.Itens);
        foreach (var item in nota.Itens)
        {
            item.Id = 0;
            item.NotaFiscalId = id;
        }
        existing.Itens = nota.Itens;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/notas/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var nota = await _db.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id);
        if (nota is null) return NotFound();
        if (nota.Status == "Impressa")
            return BadRequest(new { erro = "Nota já impressa não pode ser excluída." });

        _db.NotasFiscais.Remove(nota);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/notas/5/imprimir
    // Debita o saldo de cada item no EstoqueService e marca a nota como "Impressa"
    [HttpPost("{id}/imprimir")]
    public async Task<IActionResult> Imprimir(int id)
    {
        var nota = await _db.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id);
        if (nota is null) return NotFound();
        if (nota.Status == "Impressa")
            return BadRequest(new { erro = "Nota já foi impressa." });

        // Lista para rastrear débitos bem-sucedidos para compensação em caso de falha
        var debitosSucessos = new List<(int ProdutoId, int Quantidade)>();

        try
        {
            // Chama EstoqueService para debitar cada item
            foreach (var item in nota.Itens)
            {
                var response = await _httpClient.PatchAsync(
                    $"api/produtos/{item.ProdutoId}/debitar?quantidade={item.Quantidade}",
                    null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var detalhe = errorContent;

                    try
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("erro", out var erroProp) && erroProp.ValueKind == JsonValueKind.String)
                            detalhe = erroProp.GetString() ?? detalhe;
                        else if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                            detalhe = detailProp.GetString() ?? detalhe;
                        else if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                            detalhe = titleProp.GetString() ?? detalhe;
                    }
                    catch
                    {
                        // Não faz nada, mantém o conteúdo original
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        detalhe = $"Produto {item.ProdutoId} não encontrado.";

                    throw new Exception($"Falha ao debitar produto {item.ProdutoId}: {detalhe}");
                }

                // Adiciona à lista de débitos bem-sucedidos
                debitosSucessos.Add((item.ProdutoId, item.Quantidade));
            }

            // Se todos os débitos foram bem-sucedidos, marca a nota como impressa
            nota.Status = "Impressa";
            await _db.SaveChangesAsync();
            return Ok(nota);
        }
        catch (Exception ex)
        {
            // Em caso de falha, compensa os débitos bem-sucedidos
            foreach (var (produtoId, quantidade) in debitosSucessos)
            {
                try
                {
                    await _httpClient.PatchAsync(
                        $"api/produtos/{produtoId}/creditar?quantidade={quantidade}",
                        null);
                }
                catch
                {
                    // Log de erro de compensação, mas continua tentando os outros
                }
            }

            return BadRequest(new { erro = ex.Message });
        }
    }
}
