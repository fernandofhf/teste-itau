using System.Text;
using ComprasProgramadas.Domain.Entities;

namespace ComprasProgramadas.Infrastructure.Cotacoes;

public class CotahistParser
{
    private const int TIPREG_START = 0;
    private const int TIPREG_LEN = 2;
    private const int DATPRE_START = 2;
    private const int DATPRE_LEN = 8;
    private const int CODBDI_START = 10;
    private const int CODBDI_LEN = 2;
    private const int CODNEG_START = 12;
    private const int CODNEG_LEN = 12;
    private const int TPMERC_START = 24;
    private const int TPMERC_LEN = 3;
    private const int PREABE_START = 56;
    private const int PRECO_LEN = 13;
    private const int PREMAX_START = 69;
    private const int PREMIN_START = 82;
    private const int PREULT_START = 108;

    static CotahistParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public IEnumerable<Cotacao> ParseArquivo(string caminhoArquivo)
    {
        var resultado = new List<Cotacao>();
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            if (linha.Length < 245) continue;

            var tipoRegistro = linha.Substring(TIPREG_START, TIPREG_LEN);
            if (tipoRegistro != "01") continue;

            var codbdi = linha.Substring(CODBDI_START, CODBDI_LEN).Trim();
            if (codbdi != "02" && codbdi != "96") continue;

            var tpmerc = int.Parse(linha.Substring(TPMERC_START, TPMERC_LEN).Trim());
            if (tpmerc != 10 && tpmerc != 20) continue;

            var datpre = linha.Substring(DATPRE_START, DATPRE_LEN);
            var ticker = linha.Substring(CODNEG_START, CODNEG_LEN).Trim();

            if (!DateOnly.TryParseExact(datpre, "yyyyMMdd", out var dataPregao)) continue;

            var cotacao = new Cotacao(
                dataPregao: dataPregao,
                ticker: ticker,
                precoAbertura: ParsePreco(linha.Substring(PREABE_START, PRECO_LEN)),
                precoFechamento: ParsePreco(linha.Substring(PREULT_START, PRECO_LEN)),
                precoMaximo: ParsePreco(linha.Substring(PREMAX_START, PRECO_LEN)),
                precoMinimo: ParsePreco(linha.Substring(PREMIN_START, PRECO_LEN))
            );

            resultado.Add(cotacao);
        }

        return resultado;
    }

    public Cotacao? ObterCotacaoFechamento(string pastaCotacoes, string ticker)
    {
        if (!Directory.Exists(pastaCotacoes)) return null;

        var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f)
            .ToList();

        foreach (var arquivo in arquivos)
        {
            var cotacoes = ParseArquivo(arquivo);
            var cotacao = cotacoes
                .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.DataPregao)
                .FirstOrDefault();

            if (cotacao != null) return cotacao;
        }

        return null;
    }

    public IEnumerable<Cotacao> ObterCotacoesPorTickers(string pastaCotacoes, IEnumerable<string> tickers)
    {
        if (!Directory.Exists(pastaCotacoes)) return Enumerable.Empty<Cotacao>();

        var tickersSet = new HashSet<string>(tickers.Select(t => t.ToUpper().Trim()));

        var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
            .OrderByDescending(f => f)
            .ToList();

        if (!arquivos.Any()) return Enumerable.Empty<Cotacao>();

        var ultimoArquivo = arquivos.First();
        var cotacoes = ParseArquivo(ultimoArquivo);

        return cotacoes
            .Where(c => tickersSet.Contains(c.Ticker.ToUpper()))
            .ToList();
    }

    private static decimal ParsePreco(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor / 100m;
        return 0m;
    }
}
