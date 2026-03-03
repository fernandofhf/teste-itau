using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Entities;

public class OrdemCompra
{
    public long Id { get; private set; }
    public long ContaMasterId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public TipoMercado TipoMercado { get; private set; }
    public DateTime DataExecucao { get; private set; }

    public ICollection<Distribuicao> Distribuicoes { get; private set; } = new List<Distribuicao>();

    protected OrdemCompra() { }

    public OrdemCompra(long contaMasterId, string ticker, int quantidade, decimal precoUnitario, TipoMercado tipoMercado)
    {
        ContaMasterId = contaMasterId;
        Ticker = ticker;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        TipoMercado = tipoMercado;
        DataExecucao = DateTime.UtcNow;
    }

    public decimal ValorTotal => Quantidade * PrecoUnitario;
}
