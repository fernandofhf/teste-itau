using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Entities;

public class EventoIR
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public TipoEventoIR Tipo { get; private set; }
    public decimal ValorBase { get; private set; }
    public decimal ValorIR { get; private set; }
    public bool PublicadoKafka { get; private set; }
    public DateTime DataEvento { get; private set; }

    public Cliente? Cliente { get; private set; }

    protected EventoIR() { }

    public EventoIR(long clienteId, TipoEventoIR tipo, decimal valorBase, decimal valorIR)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        ValorBase = valorBase;
        ValorIR = valorIR;
        PublicadoKafka = false;
        DataEvento = DateTime.UtcNow;
    }

    public void MarcarPublicado()
    {
        PublicadoKafka = true;
    }
}
