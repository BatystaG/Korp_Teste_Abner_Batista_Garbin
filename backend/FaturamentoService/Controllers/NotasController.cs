using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<NotasController> _logger;

    // HttpClient é injetado via IHttpClientFactory — melhor prática para evitar
    // esgotamento de sockets ao reutilizar conexões
    public NotasController(
        FaturamentoDbContext db,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<NotasController> logger)
    {
        _db = db;
        _httpClient = httpFactory.CreateClient("EstoqueService");
        _config = config;
        _logger = logger;
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
        nota.Status = "Aberta";
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
        if (existing.Status == "Fechada")
            return BadRequest(new { erro = "Nota já fechada não pode ser alterada." });

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
        if (nota.Status == "Fechada")
            return BadRequest(new { erro = "Nota já fechada não pode ser excluída." });

        _db.NotasFiscais.Remove(nota);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<HttpResponseMessage> SendPatchWithRetryAsync(string uri, int maxAttempts = 2)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.PatchAsync(uri, null);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Falha de comunicação com EstoqueService na tentativa {Attempt} para {Uri}", attempt, uri);
                if (attempt == maxAttempts)
                    throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout ao chamar EstoqueService na tentativa {Attempt} para {Uri}", attempt, uri);
                if (attempt == maxAttempts)
                    throw;
            }

            await Task.Delay(200);
        }

        throw new InvalidOperationException("Tentativa de requisição excedeu o número máximo de tentativas.");
    }

    private static string ExtractErrorDetail(string content, HttpStatusCode statusCode, int produtoId)
    {
        var detalhe = content;

        try
        {
            using var doc = JsonDocument.Parse(content);
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
            // mantém o conteúdo original se não for JSON válido
        }

        if (statusCode == HttpStatusCode.NotFound)
            detalhe = $"Produto {produtoId} não encontrado.";

        return detalhe;
    }

    private async Task<bool> CompensateDebitAsync(int produtoId, int quantidade)
    {
        const int retryCount = 2;
        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"api/produtos/{produtoId}/creditar?quantidade={quantidade}", null);
                if (response.IsSuccessStatusCode)
                    return true;

                _logger.LogWarning("Compensação falhou para produto {ProdutoId}: status {StatusCode}", produtoId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exceção ao tentar compensar débito para produto {ProdutoId} na tentativa {Attempt}", produtoId, attempt);
            }

            await Task.Delay(200);
        }

        return false;
    }

    [HttpPost("{id}/imprimir")]
    public async Task<IActionResult> Imprimir(int id)
    {
        var nota = await _db.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id);
        if (nota is null) return NotFound();
        if (nota.Status == "Fechada")
            return BadRequest(new { erro = "Nota já foi fechada." });

        var debitosSucessos = new List<(int ProdutoId, int Quantidade)>();

        try
        {
            foreach (var item in nota.Itens)
            {
                var uri = $"api/produtos/{item.ProdutoId}/debitar?quantidade={item.Quantidade}";
                HttpResponseMessage response;

                try
                {
                    response = await SendPatchWithRetryAsync(uri);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro irreversível ao comunicar EstoqueService ao debitar produto {ProdutoId}", item.ProdutoId);
                    return StatusCode(502, new { erro = "Erro de comunicação com o serviço de estoque. Tente novamente em alguns instantes." });
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var detalhe = ExtractErrorDetail(errorContent, response.StatusCode, item.ProdutoId);
                    throw new Exception($"Falha ao debitar produto {item.ProdutoId}: {detalhe}");
                }

                debitosSucessos.Add((item.ProdutoId, item.Quantidade));
            }

            nota.Status = "Fechada";
            await _db.SaveChangesAsync();
            return Ok(nota);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao imprimir nota {NotaId}, tentando compensar débitos", nota.Id);
            var compensationFailures = new List<int>();

            foreach (var (produtoId, quantidade) in debitosSucessos)
            {
                var compensated = await CompensateDebitAsync(produtoId, quantidade);
                if (!compensated)
                {
                    compensationFailures.Add(produtoId);
                }
            }

            if (compensationFailures.Any())
            {
                _logger.LogError("Falha de compensação para produtos {ProdutoIds} na nota {NotaId}", compensationFailures, nota.Id);
                return StatusCode(500, new { erro = "Erro ao recuperar o estoque após falha. Verifique os logs do sistema." });
            }

            return BadRequest(new { erro = ex.Message });
        }
    }
}
