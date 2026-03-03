namespace ComprasProgramadas.Domain.Entities;

public class Distribuicao
{
    public long Id { get; private set; }
    public long OrdemCompraId { get; private set; }
    public long CustodiaFilhoteId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public DateTime DataDistribuicao { get; private set; }

    public OrdemCompra? OrdemCompra { get; private set; }
    public Custodia? CustodiaFilhote { get; private set; }

    protected Distribuicao() { }

    public Distribuicao(long ordemCompraId, long custodiaFilhoteId, string ticker, int quantidade, decimal precoUnitario)
    {
        OrdemCompraId = ordemCompraId;
        CustodiaFilhoteId = custodiaFilhoteId;
        Ticker = ticker;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        DataDistribuicao = DateTime.UtcNow;
    }

    public decimal ValorOperacao => Quantidade * PrecoUnitario;

    public decimal CalcularIRDedoDuro() => Math.Round(ValorOperacao * 0.00005m, 2);
}
