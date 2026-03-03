using ComprasProgramadas.Application.Commands.Admin;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ComprasProgramadas.API.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

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

    /// <summary>Consultar custódia da conta master (resíduos das distribuições)</summary>
    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(typeof(CustodiaMasterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CustodiaMaster(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustodiaMasterQuery(), ct);
        return Ok(result);
    }
}
