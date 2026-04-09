namespace EstoqueService.Models;

public class Produto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Saldo { get; set; }

    // RowVersion para controle de concorrência otimista
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
