namespace ComprasProgramadas.Domain.Entities;

public class HistoricoAporte
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public DateTime DataAlteracao { get; private set; }

    public Cliente? Cliente { get; private set; }

    protected HistoricoAporte() { }

    public HistoricoAporte(long clienteId, decimal valorAnterior, decimal valorNovo)
    {
        ClienteId = clienteId;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        DataAlteracao = DateTime.UtcNow;
    }
}
