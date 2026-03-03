using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Entities;

public class Rebalanceamento
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public TipoRebalanceamento Tipo { get; private set; }
    public string? TickerVendido { get; private set; }
    public string? TickerComprado { get; private set; }
    public decimal ValorVenda { get; private set; }
    public DateTime DataRebalanceamento { get; private set; }

    public Cliente? Cliente { get; private set; }

    protected Rebalanceamento() { }

    public Rebalanceamento(long clienteId, TipoRebalanceamento tipo, string? tickerVendido, string? tickerComprado, decimal valorVenda)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        TickerVendido = tickerVendido;
        TickerComprado = tickerComprado;
        ValorVenda = valorVenda;
        DataRebalanceamento = DateTime.UtcNow;
    }
}
