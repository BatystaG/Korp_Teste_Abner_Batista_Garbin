using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ProdutosController> _logger;

    public ProdutosController(EstoqueDbContext db, ILogger<ProdutosController> logger)
    {
        _db = db;
        _logger = logger;
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
        if (produto is null)
        {
            _logger.LogInformation("Produto {ProdutoId} não encontrado.", id);
            return NotFound(new { erro = "Produto não encontrado." });
        }
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
        if (id != produto.Id)
        {
            _logger.LogWarning("Produto id informado ({ProvidedId}) difere do id do payload ({PayloadId}).", id, produto.Id);
            return BadRequest(new { erro = "Id da rota e id do produto devem ser iguais." });
        }

        _db.Entry(produto).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/produtos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
        {
            _logger.LogInformation("Tentativa de excluir produto {ProdutoId} não encontrado.", id);
            return NotFound(new { erro = "Produto não encontrado." });
        }

        _db.Produtos.Remove(produto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/produtos/5/debitar?quantidade=10
    // Chamado pelo FaturamentoService ao imprimir uma NF
    [HttpPatch("{id}/debitar")]
    public async Task<IActionResult> Debitar(int id, [FromQuery] int quantidade)
    {
        if (quantidade <= 0)
            return BadRequest(new { erro = "Quantidade deve ser maior que zero." });

        // Implementa concorrência otimista com retry
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var produto = await _db.Produtos.FindAsync(id);
                if (produto is null)
                {
                    _logger.LogInformation("Tentativa de debitar produto {ProdutoId} não encontrado.", id);
                    return NotFound(new { erro = "Produto não encontrado." });
                }

                if (produto.Saldo < quantidade)
                {
                    _logger.LogInformation("Saldo insuficiente para produto {ProdutoId}. Saldo atual={Saldo}, solicitado={Quantidade}.", id, produto.Saldo, quantidade);
                    return BadRequest(new { erro = "Saldo insuficiente." });
                }

                produto.Saldo -= quantidade;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Saldo debitado com sucesso para produto {ProdutoId}. Quantidade={Quantidade}, novo saldo={Saldo}.", id, quantidade, produto.Saldo);
                return Ok(produto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflito de concorrência na tentativa {Attempt} para produto {ProdutoId}.", attempt + 1, id);

                // Se foi a última tentativa, retorna erro
                if (attempt == maxRetries - 1)
                {
                    _logger.LogError("Falha definitiva por concorrência para produto {ProdutoId} após {MaxRetries} tentativas.", id, maxRetries);
                    return Conflict(new { erro = "Produto está sendo modificado por outra operação. Tente novamente." });
                }

                // Recarrega o contexto para a próxima tentativa
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                // Pequena pausa antes de tentar novamente
                await Task.Delay(100 * (attempt + 1));
            }
        }

        // Este código nunca deve ser alcançado, mas é boa prática ter
        return StatusCode(500, new { erro = "Erro interno do servidor." });
    }

    // PATCH /api/produtos/5/creditar?quantidade=10
    // Chamado pelo FaturamentoService para compensar débito em caso de falha
    [HttpPatch("{id}/creditar")]
    public async Task<IActionResult> Creditar(int id, [FromQuery] int quantidade)
    {
        if (quantidade <= 0)
            return BadRequest(new { erro = "Quantidade deve ser maior que zero." });

        // Implementa concorrência otimista com retry
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var produto = await _db.Produtos.FindAsync(id);
                if (produto is null)
                {
                    _logger.LogInformation("Tentativa de creditar produto {ProdutoId} não encontrado.", id);
                    return NotFound(new { erro = "Produto não encontrado." });
                }

                produto.Saldo += quantidade;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Saldo creditado com sucesso para produto {ProdutoId}. Quantidade={Quantidade}, novo saldo={Saldo}.", id, quantidade, produto.Saldo);
                return Ok(produto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Conflito de concorrência na tentativa {Attempt} para produto {ProdutoId}.", attempt + 1, id);

                // Se foi a última tentativa, retorna erro
                if (attempt == maxRetries - 1)
                {
                    _logger.LogError("Falha definitiva por concorrência para produto {ProdutoId} após {MaxRetries} tentativas.", id, maxRetries);
                    return Conflict(new { erro = "Produto está sendo modificado por outra operação. Tente novamente." });
                }

                // Recarrega o contexto para a próxima tentativa
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                // Pequena pausa antes de tentar novamente
                await Task.Delay(100 * (attempt + 1));
            }
        }

        // Este código nunca deve ser alcançado, mas é boa prática ter
        return StatusCode(500, new { erro = "Erro interno do servidor." });
    }
}
