namespace ComprasProgramadas.Domain.Entities;

public class Cliente
{
    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string CPF { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public decimal ValorMensal { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime DataAdesao { get; private set; }
    public DateTime? DataSaida { get; private set; }

    public ContaGrafica? ContaGrafica { get; private set; }

    protected Cliente() { }

    public Cliente(string nome, string cpf, string email, decimal valorMensal)
    {
        Nome = nome;
        CPF = cpf;
        Email = email;
        ValorMensal = valorMensal;
        Ativo = true;
        DataAdesao = DateTime.UtcNow;
    }

    public void Sair()
    {
        Ativo = false;
        DataSaida = DateTime.UtcNow;
    }

    public void AlterarValorMensal(decimal novoValor)
    {
        ValorMensal = novoValor;
    }

    public decimal CalcularValorParcela()
    {
        return Math.Truncate(ValorMensal / 3 * 100) / 100;
    }
}
