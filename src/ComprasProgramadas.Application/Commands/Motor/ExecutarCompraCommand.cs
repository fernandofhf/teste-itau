using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Services.Interfaces;
using MediatR;

namespace ComprasProgramadas.Application.Commands.Motor;

public record ExecutarCompraCommand(DateOnly? DataReferencia = null) : IRequest<ExecutarCompraResponse>;

public class ExecutarCompraHandler : IRequestHandler<ExecutarCompraCommand, ExecutarCompraResponse>
{
    private readonly IMotorCompraService _motorCompraService;

    public ExecutarCompraHandler(IMotorCompraService motorCompraService) => _motorCompraService = motorCompraService;

    public async Task<ExecutarCompraResponse> Handle(ExecutarCompraCommand request, CancellationToken ct)
    {
        var data = request.DataReferencia ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return await _motorCompraService.ExecutarCompraAsync(data, ct);
    }
}
