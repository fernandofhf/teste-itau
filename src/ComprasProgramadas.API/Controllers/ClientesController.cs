using ComprasProgramadas.Application.Commands.Clientes;
using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Queries.Clientes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ComprasProgramadas.API.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Adesão ao produto de compra programada</summary>
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(AdesaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AderirProdutoCommand(request.Nome, request.CPF, request.Email, request.ValorMensal), ct);
        return CreatedAtAction(nameof(Carteira), new { clienteId = result.ClienteId }, result);
    }

    /// <summary>Saída do produto (mantém a posição em custódia)</summary>
    [HttpPost("{clienteId:long}/saida")]
    [ProducesResponseType(typeof(SaidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sair(long clienteId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SairProdutoCommand(clienteId), ct);
        return Ok(result);
    }

    /// <summary>Alterar valor mensal de aporte</summary>
    [HttpPut("{clienteId:long}/valor-mensal")]
    [ProducesResponseType(typeof(AlterarValorMensalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarValorMensal(long clienteId, [FromBody] AlterarValorMensalRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AlterarValorMensalCommand(clienteId, request.NovoValorMensal), ct);
        return Ok(result);
    }

    /// <summary>Consultar carteira do cliente (custódia, P/L, rentabilidade)</summary>
    [HttpGet("{clienteId:long}/carteira")]
    [ProducesResponseType(typeof(CarteiraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Carteira(long clienteId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCarteiraQuery(clienteId), ct);
        return Ok(result);
    }

    /// <summary>Consultar histórico de ordens executadas para o cliente (compras do motor + rebalanceamentos)</summary>
    [HttpGet("{clienteId:long}/ordens")]
    [ProducesResponseType(typeof(OrdensClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OrdensCliente(long clienteId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrdensClienteQuery(clienteId), ct);
        return Ok(result);
    }

    /// <summary>Consultar histórico de alterações do valor mensal de aporte</summary>
    [HttpGet("{clienteId:long}/historico-aportes")]
    [ProducesResponseType(typeof(HistoricoAportesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HistoricoAportes(long clienteId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHistoricoAportesQuery(clienteId), ct);
        return Ok(result);
    }

    /// <summary>Consultar rentabilidade detalhada (histórico de aportes e evolução)</summary>
    [HttpGet("{clienteId:long}/rentabilidade")]
    [ProducesResponseType(typeof(RentabilidadeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rentabilidade(long clienteId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRentabilidadeQuery(clienteId), ct);
        return Ok(result);
    }
}
