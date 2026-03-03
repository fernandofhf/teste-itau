namespace ComprasProgramadas.Domain.Entities;

public class Cotacao
{
    public long Id { get; private set; }
    public DateOnly DataPregao { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public decimal PrecoAbertura { get; private set; }
    public decimal PrecoFechamento { get; private set; }
    public decimal PrecoMaximo { get; private set; }
    public decimal PrecoMinimo { get; private set; }

    protected Cotacao() { }

    public Cotacao(DateOnly dataPregao, string ticker, decimal precoAbertura, decimal precoFechamento, decimal precoMaximo, decimal precoMinimo)
    {
        DataPregao = dataPregao;
        Ticker = ticker.Trim().ToUpper();
        PrecoAbertura = precoAbertura;
        PrecoFechamento = precoFechamento;
        PrecoMaximo = precoMaximo;
        PrecoMinimo = precoMinimo;
    }
}
