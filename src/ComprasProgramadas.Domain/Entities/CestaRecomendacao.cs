namespace ComprasProgramadas.Domain.Entities;

public class CestaRecomendacao
{
    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataDesativacao { get; private set; }

    public ICollection<ItemCesta> Itens { get; private set; } = new List<ItemCesta>();

    protected CestaRecomendacao() { }

    public CestaRecomendacao(string nome, IEnumerable<ItemCesta> itens)
    {
        Nome = nome;
        Ativa = true;
        DataCriacao = DateTime.UtcNow;
        Itens = itens.ToList();
    }

    public void Desativar()
    {
        Ativa = false;
        DataDesativacao = DateTime.UtcNow;
    }
}
