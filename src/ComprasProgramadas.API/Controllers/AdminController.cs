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

        // Deletar na ordem correta respeitando FKs
        await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Distribuicoes;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE EventosIR;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Rebalanceamentos;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE OrdensCompra;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Custodias;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE ContasGraficas;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Clientes;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE ItensCesta;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE CestasRecomendacao;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Cotacoes;", ct);
        await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE HistoricoAportes;", ct);
        await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", ct);

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
}
