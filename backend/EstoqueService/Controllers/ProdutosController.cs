using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;
using EstoqueService.Models;

namespace EstoqueService.Controllers;

// [ApiController] habilita validações automáticas e respostas HTTP padronizadas.
// [Route] define o prefixo da URL: /api/produtos
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _db;

    public ProdutosController(EstoqueDbContext db)
    {
        _db = db;
    }

    // GET /api/produtos
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var produtos = await _db.Produtos.ToListAsync();
        return Ok(produtos);
    }

    // GET /api/produtos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null) return NotFound();
        return Ok(produto);
    }

    // POST /api/produtos
    [HttpPost]
    public async Task<IActionResult> Create(Produto produto)
    {
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();
        // Retorna 201 Created com o header Location apontando para o novo recurso
        return CreatedAtAction(nameof(GetById), new { id = produto.Id }, produto);
    }

    // PUT /api/produtos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Produto produto)
    {
        if (id != produto.Id) return BadRequest();
        _db.Entry(produto).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/produtos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null) return NotFound();
        _db.Produtos.Remove(produto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/produtos/5/debitar?quantidade=10
    // Chamado pelo FaturamentoService ao imprimir uma NF
    [HttpPatch("{id}/debitar")]
    public async Task<IActionResult> Debitar(int id, [FromQuery] int quantidade)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null) return NotFound();
        if (produto.Saldo < quantidade)
            return BadRequest(new { erro = "Saldo insuficiente." });

        produto.Saldo -= quantidade;
        await _db.SaveChangesAsync();
        return Ok(produto);
    }
}
