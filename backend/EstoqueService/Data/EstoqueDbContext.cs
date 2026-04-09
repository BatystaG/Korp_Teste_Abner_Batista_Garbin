using Microsoft.EntityFrameworkCore;
using EstoqueService.Models;

namespace EstoqueService.Data;

// DbContext é a classe do EF Core que representa a sessão com o banco.
// Cada DbSet<T> vira uma tabela no PostgreSQL.
public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configura RowVersion como token de concorrência
        modelBuilder.Entity<Produto>()
            .Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
