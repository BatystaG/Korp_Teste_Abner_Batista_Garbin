namespace FaturamentoService.Models;

public class ItemNota
{
    public int Id { get; set; }

    // Chave estrangeira para NotaFiscal (EF Core detecta pelo nome convencional)
    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    // Referência ao produto que existe no EstoqueService
    // (não é FK real entre bancos — apenas armazenamos o Id)
    public int ProdutoId { get; set; }
    public string ProdutoDescricao { get; set; } = string.Empty;

    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
