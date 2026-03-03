namespace ComprasProgramadas.Domain.Entities;

public class Custodia
{
    public long Id { get; private set; }
    public long ContaGraficaId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public DateTime DataUltimaAtualizacao { get; private set; }

    public ContaGrafica? ContaGrafica { get; private set; }

    protected Custodia() { }

    public Custodia(long contaGraficaId, string ticker)
    {
        ContaGraficaId = contaGraficaId;
        Ticker = ticker;
        Quantidade = 0;
        PrecoMedio = 0;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public void AdicionarAtivos(int quantidade, decimal precoCompra)
    {
        if (quantidade <= 0) return;

        var novoPrecoMedio = Quantidade == 0
            ? precoCompra
            : (Quantidade * PrecoMedio + quantidade * precoCompra) / (Quantidade + quantidade);

        Quantidade += quantidade;
        PrecoMedio = novoPrecoMedio;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public void RemoverAtivos(int quantidade)
    {
        if (quantidade <= 0 || quantidade > Quantidade)
            throw new InvalidOperationException($"Quantidade insuficiente de {Ticker}. Disponível: {Quantidade}, solicitado: {quantidade}");

        Quantidade -= quantidade;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public decimal CalcularValorAtual(decimal cotacaoAtual) => Quantidade * cotacaoAtual;

    public decimal CalcularPL(decimal cotacaoAtual) => (cotacaoAtual - PrecoMedio) * Quantidade;

    public decimal CalcularCustoTotal() => PrecoMedio * Quantidade;
}
