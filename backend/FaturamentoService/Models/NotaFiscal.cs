namespace FaturamentoService.Models;

public class NotaFiscal
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;

    // "Aberta" = ainda editável | "Fechada" = saldo já debitado, bloqueada
    public string Status { get; set; } = "Aberta";

    // Controle de idempotência para impressão
    public string? ImpressaoToken { get; set; }

    // Navegação: uma NF tem muitos itens
    public List<ItemNota> Itens { get; set; } = new();
}
