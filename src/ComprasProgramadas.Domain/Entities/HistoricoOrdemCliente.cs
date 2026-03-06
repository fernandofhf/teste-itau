using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Entities;

public class HistoricoOrdemCliente
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public string Ticker { get; private set; } = null!;
    public TipoOrdem TipoOrdem { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal ValorTotal { get; private set; }
    public OrigemOrdem Origem { get; private set; }
    public DateTime DataOperacao { get; private set; }

    public Cliente? Cliente { get; private set; }

    protected HistoricoOrdemCliente() { }

    public HistoricoOrdemCliente(long clienteId, string ticker, TipoOrdem tipoOrdem,
        int quantidade, decimal precoUnitario, OrigemOrdem origem)
    {
        ClienteId = clienteId;
        Ticker = ticker;
        TipoOrdem = tipoOrdem;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        ValorTotal = quantidade * precoUnitario;
        Origem = origem;
        DataOperacao = DateTime.UtcNow;
    }
}
