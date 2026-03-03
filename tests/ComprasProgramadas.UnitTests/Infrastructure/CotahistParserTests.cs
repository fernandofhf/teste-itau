using ComprasProgramadas.Infrastructure.Cotacoes;
using FluentAssertions;

namespace ComprasProgramadas.UnitTests.Infrastructure;

public class CotahistParserTests : IDisposable
{
    private readonly CotahistParser _parser;
    private readonly string _tempDir;

    public CotahistParserTests()
    {
        _parser = new CotahistParser();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static string CriarLinhaHeader() =>
        "00COTAHIST.2026BOVESPA 20260225                                                                                                                                                                                                   ";

    private static string CriarLinhaCotacao(string ticker, string codbdi, string tpmerc, string preco)
    {
        // Layout COTAHIST: linha 245 chars, ISO-8859-1
        // TIPREG[0:2], DATPRE[2:10], CODBDI[10:12], CODNEG[12:24], TPMERC[24:27]
        // ...PREABE[56:69], PREMAX[69:82], PREMIN[82:95], PREMED[95:108], PREULT[108:121]
        var linha = new char[245];
        Array.Fill(linha, ' ');

        // TIPREG = "01"
        linha[0] = '0'; linha[1] = '1';
        // DATPRE = "20260225"
        "20260225".CopyTo(0, linha, 2, 8);
        // CODBDI
        codbdi.PadRight(2).CopyTo(0, linha, 10, 2);
        // CODNEG (ticker, 12 chars)
        ticker.PadRight(12).CopyTo(0, linha, 12, 12);
        // TPMERC
        tpmerc.PadLeft(3).CopyTo(0, linha, 24, 3);
        // Preços (13 chars cada, valor sem vírgula, divide por 100)
        // PREABE[56], PREMAX[69], PREMIN[82], PREMED[95], PREULT[108]
        var precoStr = preco.PadLeft(13, '0');
        precoStr.CopyTo(0, linha, 56, 13);  // PREABE
        precoStr.CopyTo(0, linha, 69, 13);  // PREMAX
        precoStr.CopyTo(0, linha, 82, 13);  // PREMIN
        precoStr.CopyTo(0, linha, 95, 13);  // PREMED
        precoStr.CopyTo(0, linha, 108, 13); // PREULT

        return new string(linha);
    }

    private string CriarArquivoCotahist(params string[] linhas)
    {
        var arquivo = Path.Combine(_tempDir, "COTAHIST_D20260225.TXT");
        var conteudo = new List<string> { CriarLinhaHeader() };
        conteudo.AddRange(linhas);
        // Trailer
        conteudo.Add("99COTAHIST.2026BOVESPA 20260225".PadRight(245));
        File.WriteAllLines(arquivo, conteudo, System.Text.Encoding.Latin1);
        return arquivo;
    }

    [Fact]
    public void ParseArquivo_FiltraApenasRegistrosTipo01()
    {
        var linhaValida = CriarLinhaCotacao("PETR4", "02", "010", "0000000350000");
        var arquivo = CriarArquivoCotahist(linhaValida);

        var cotacoes = _parser.ParseArquivo(arquivo).ToList();

        cotacoes.Should().HaveCount(1);
        cotacoes[0].Ticker.Should().Be("PETR4");
    }

    [Fact]
    public void ParseArquivo_ConvertePrecoComDoisDecimaisImplicitos()
    {
        // Preco "0000000350000" → 350000 / 100 = R$ 3500.00
        var linha = CriarLinhaCotacao("VALE3", "02", "010", "0000000350000");
        var arquivo = CriarArquivoCotahist(linha);

        var cotacoes = _parser.ParseArquivo(arquivo).ToList();

        cotacoes[0].PrecoFechamento.Should().Be(3500.00m);
    }

    [Fact]
    public void ParseArquivo_TickerFracionario_FiltraComCodbdi96()
    {
        var linha = CriarLinhaCotacao("PETR4F", "96", "020", "0000000350000");
        var arquivo = CriarArquivoCotahist(linha);

        var cotacoes = _parser.ParseArquivo(arquivo).ToList();

        cotacoes.Should().HaveCount(1);
        cotacoes[0].Ticker.Should().Be("PETR4F");
    }

    [Fact]
    public void ParseArquivo_FiltraCodbdiInvalido()
    {
        // CODBDI = "10" não é filtrado (apenas "02" e "96" passam)
        var linha = CriarLinhaCotacao("XXXX3", "10", "010", "0000000350000");
        var arquivo = CriarArquivoCotahist(linha);

        var cotacoes = _parser.ParseArquivo(arquivo).ToList();

        cotacoes.Should().BeEmpty();
    }

    [Fact]
    public void ObterCotacoesPorTickers_PastaVazia_RetornaEmpty()
    {
        var cotacoes = _parser.ObterCotacoesPorTickers(_tempDir, new[] { "PETR4" });

        cotacoes.Should().BeEmpty();
    }

    [Fact]
    public void ObterCotacoesPorTickers_PastaInexistente_RetornaEmpty()
    {
        var cotacoes = _parser.ObterCotacoesPorTickers("/nao/existe", new[] { "PETR4" });

        cotacoes.Should().BeEmpty();
    }

    [Fact]
    public void ObterCotacoesPorTickers_FiltrarPorTicker_RetornaApenasSolicitados()
    {
        var petr4 = CriarLinhaCotacao("PETR4", "02", "010", "0000000350000");
        var vale3 = CriarLinhaCotacao("VALE3", "02", "010", "0000007200000");
        CriarArquivoCotahist(petr4, vale3);

        var cotacoes = _parser.ObterCotacoesPorTickers(_tempDir, new[] { "PETR4" }).ToList();

        cotacoes.Should().HaveCount(1);
        cotacoes[0].Ticker.Should().Be("PETR4");
    }
}
