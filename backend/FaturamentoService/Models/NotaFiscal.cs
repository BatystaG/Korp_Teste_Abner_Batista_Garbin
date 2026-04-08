namespace FaturamentoService.Models;

public class NotaFiscal
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;

    // "Rascunho" = ainda editável | "Impressa" = saldo já debitado, bloqueada
    public string Status { get; set; } = "Rascunho";

    // Navegação: uma NF tem muitos itens
    public List<ItemNota> Itens { get; set; } = new();
}
