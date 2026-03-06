using ComprasProgramadas.Application.Commands.Admin;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Queries.Admin;
using ComprasProgramadas.Application.Services.Interfaces;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.API.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IRebalanceamentoService _rebalanceamentoService;
    private readonly ICestaRecomendacaoRepository _cestaRepo;
    private readonly IKafkaConsumer _kafkaConsumer;

    public AdminController(IMediator mediator, AppDbContext context, IWebHostEnvironment env,
        IRebalanceamentoService rebalanceamentoService, ICestaRecomendacaoRepository cestaRepo,
        IKafkaConsumer kafkaConsumer)
    {
        _mediator = mediator;
        _context = context;
        _env = env;
        _rebalanceamentoService = rebalanceamentoService;
        _cestaRepo = cestaRepo;
        _kafkaConsumer = kafkaConsumer;
    }

    /// <summary>Cadastrar ou alterar a cesta Top Five (dispara rebalanceamento se existir cesta anterior)</summary>
    [HttpPost("cesta")]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarCesta([FromBody] CriarCestaRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CriarCestaCommand(request.Nome, request.Itens), ct);
        return CreatedAtAction(nameof(CestaAtual), result);
    }

    /// <summary>Consultar a cesta de recomendação ativa</summary>
    [HttpGet("cesta/atual")]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CestaAtual(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCestaAtualQuery(), ct);
        return Ok(result);
    }

    /// <summary>Consultar histórico de cestas de recomendação</summary>
    [HttpGet("cesta/historico")]
    [ProducesResponseType(typeof(HistoricoCestasResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> HistoricoCestas(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHistoricoCestasQuery(), ct);
        return Ok(result);
    }

    /// <summary>[DEV ONLY] Zera toda a base de dados e recria a conta master. Use apenas para testes.</summary>
    [HttpDelete("reset-database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetDatabase(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return Forbid();

        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        await using (var cmd = conn.CreateCommand())
        {
            var tabelas = new[]
            {
                "Distribuicoes", "EventosIR", "Rebalanceamentos", "OrdensCompra",
                "Custodias", "HistoricoAportes", "ContasGraficas", "Clientes",
                "ItensCesta", "CestasRecomendacao", "Cotacoes", "HistoricoOrdensCliente"
            };

            cmd.CommandText = "SET FOREIGN_KEY_CHECKS = 0;";
            await cmd.ExecuteNonQueryAsync(ct);

            foreach (var tabela in tabelas)
            {
                cmd.CommandText = $"TRUNCATE TABLE {tabela};";
                await cmd.ExecuteNonQueryAsync(ct);
            }

            cmd.CommandText = "SET FOREIGN_KEY_CHECKS = 1;";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await conn.CloseAsync();

        // Recriar conta master
        var master = ContaGrafica.CriarMaster();
        _context.ContasGraficas.Add(master);
        await _context.SaveChangesAsync(ct);

        return Ok(new { mensagem = "Base zerada com sucesso. Conta Master recriada.", contaMasterId = master.Id });
    }

    /// <summary>[RN-050/051/052] Executar rebalanceamento por desvio de proporção (limiar padrão: 5pp)</summary>
    [HttpPost("rebalanceamento/desvio")]
    [ProducesResponseType(typeof(ExecutarRebalanceamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RebalanceamentoPorDesvio([FromQuery] decimal limiar = 5m, CancellationToken ct = default)
    {
        var cesta = await _cestaRepo.ObterAtivaAsync(ct);
        if (cesta == null)
            return NotFound(new { erro = "Nenhuma cesta ativa encontrada." });

        var result = await _rebalanceamentoService.ExecutarRebalanceamentoPorDesvioAsync(cesta, limiar, ct);
        return Ok(result);
    }

    /// <summary>Listar todas as mensagens publicadas no tópico Kafka (desde o offset 0)</summary>
    [HttpGet("kafka/mensagens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult MensagensKafka([FromQuery] string topico = "ir-eventos", [FromQuery] int timeoutMs = 2000)
    {
        var mensagens = _kafkaConsumer.LerTodasMensagens(topico, timeoutMs);

        return Ok(new
        {
            topico,
            total = mensagens.Count,
            mensagens = mensagens.Select(m => new
            {
                m.Offset,
                m.Particao,
                m.Timestamp,
                conteudo = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(m.Conteudo)
            })
        });
    }

    /// <summary>Consultar custódia da conta master (resíduos das distribuições)</summary>
    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(typeof(CustodiaMasterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CustodiaMaster(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustodiaMasterQuery(), ct);
        return Ok(result);
    }

    /// <summary>[DEV] Listar todos os registros de uma tabela para inspeção</summary>
    [HttpGet("tabelas/{tabela}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTabela(string tabela, CancellationToken ct)
    {
        object? data = tabela.ToLowerInvariant() switch
        {
            "clientes" => await _context.Clientes.AsNoTracking()
                .Select(x => new { x.Id, x.Nome, x.CPF, x.Email, x.ValorMensal, x.Ativo, x.DataAdesao, x.DataSaida })
                .ToListAsync(ct),

            "contasgraficas" => await _context.ContasGraficas.AsNoTracking()
                .Select(x => new { x.Id, x.ClienteId, x.NumeroConta, Tipo = x.Tipo.ToString(), x.DataCriacao })
                .ToListAsync(ct),

            "custodias" => await _context.Custodias.AsNoTracking()
                .Select(x => new { x.Id, x.ContaGraficaId, x.Ticker, x.Quantidade, x.PrecoMedio, x.DataUltimaAtualizacao })
                .ToListAsync(ct),

            "cestasrecomendacao" => await _context.CestasRecomendacao.AsNoTracking()
                .Select(x => new { x.Id, x.Nome, x.Ativa, x.DataCriacao, x.DataDesativacao })
                .ToListAsync(ct),

            "itenscesta" => await _context.ItensCesta.AsNoTracking()
                .Select(x => new { x.Id, x.CestaId, x.Ticker, x.Percentual })
                .ToListAsync(ct),

            "ordenscompra" => await _context.OrdensCompra.AsNoTracking()
                .Select(x => new { x.Id, x.ContaMasterId, x.Ticker, x.Quantidade, x.PrecoUnitario, TipoMercado = x.TipoMercado.ToString(), x.DataExecucao })
                .ToListAsync(ct),

            "distribuicoes" => await _context.Distribuicoes.AsNoTracking()
                .Select(x => new { x.Id, x.OrdemCompraId, x.CustodiaFilhoteId, x.Ticker, x.Quantidade, x.PrecoUnitario, x.DataDistribuicao })
                .ToListAsync(ct),

            "eventosir" => await _context.EventosIR.AsNoTracking()
                .Select(x => new { x.Id, x.ClienteId, Tipo = x.Tipo.ToString(), x.ValorBase, x.ValorIR, x.PublicadoKafka, x.DataEvento })
                .ToListAsync(ct),

            "cotacoes" => await _context.Cotacoes.AsNoTracking()
                .Select(x => new { x.Id, x.DataPregao, x.Ticker, x.PrecoAbertura, x.PrecoFechamento, x.PrecoMaximo, x.PrecoMinimo })
                .OrderByDescending(x => x.DataPregao)
                .ToListAsync(ct),

            "rebalanceamentos" => await _context.Rebalanceamentos.AsNoTracking()
                .Select(x => new { x.Id, x.ClienteId, Tipo = x.Tipo.ToString(), x.TickerVendido, x.TickerComprado, x.ValorVenda, x.DataRebalanceamento })
                .ToListAsync(ct),

            "historicoaportes" => await _context.HistoricoAportes.AsNoTracking()
                .Select(x => new { x.Id, x.ClienteId, x.ValorAnterior, x.ValorNovo, x.DataAlteracao })
                .ToListAsync(ct),

            "historicoordenscliente" => await _context.HistoricoOrdensCliente.AsNoTracking()
                .Select(x => new { x.Id, x.ClienteId, x.Ticker, TipoOrdem = x.TipoOrdem.ToString(), x.Quantidade, x.PrecoUnitario, x.ValorTotal, Origem = x.Origem.ToString(), x.DataOperacao })
                .ToListAsync(ct),

            _ => null
        };

        if (data is null)
            return NotFound(new { erro = $"Tabela '{tabela}' não encontrada.", disponiveis = new[] { "clientes", "contasgraficas", "custodias", "cestasrecomendacao", "itenscesta", "ordenscompra", "distribuicoes", "eventosir", "cotacoes", "rebalanceamentos", "historicoaportes", "historicoordenscliente" } });

        return Ok(data);
    }
}
