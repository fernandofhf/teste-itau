using ComprasProgramadas.Domain.Enums;

namespace ComprasProgramadas.Domain.Entities;

public class ContaGrafica
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public string NumeroConta { get; private set; } = string.Empty;
    public TipoConta Tipo { get; private set; }
    public DateTime DataCriacao { get; private set; }

    public Cliente? Cliente { get; private set; }
    public ICollection<Custodia> Custodias { get; private set; } = new List<Custodia>();

    protected ContaGrafica() { }

    public ContaGrafica(long clienteId, string numeroConta, TipoConta tipo)
    {
        ClienteId = clienteId;
        NumeroConta = numeroConta;
        Tipo = tipo;
        DataCriacao = DateTime.UtcNow;
    }

    public static ContaGrafica CriarMaster()
    {
        return new ContaGrafica(0, "MST-000001", TipoConta.Master)
        {
            ClienteId = 0
        };
    }

    public static string GerarNumeroFilhote(long clienteId)
    {
        return $"FLH-{clienteId:D6}";
    }
}
