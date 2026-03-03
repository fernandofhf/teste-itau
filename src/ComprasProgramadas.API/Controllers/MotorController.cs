using ComprasProgramadas.Application.Commands.Motor;
using ComprasProgramadas.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ComprasProgramadas.API.Controllers;

[ApiController]
[Route("api/motor")]
[Produces("application/json")]
public class MotorController : ControllerBase
{
    private readonly IMediator _mediator;

    public MotorController(IMediator mediator) => _mediator = mediator;

    /// <summary>Executar o motor de compra programada manualmente (para testes e demonstração)</summary>
    [HttpPost("executar-compra")]
    [ProducesResponseType(typeof(ExecutarCompraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExecutarCompraCommand(request.DataReferencia), ct);
        return Ok(result);
    }
}
