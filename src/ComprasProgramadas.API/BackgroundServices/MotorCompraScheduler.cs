using ComprasProgramadas.Application.Services.Interfaces;

namespace ComprasProgramadas.API.BackgroundServices;

public class MotorCompraScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MotorCompraScheduler> _logger;

    public MotorCompraScheduler(IServiceScopeFactory scopeFactory, ILogger<MotorCompraScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Motor de Compra iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var agora = DateTime.UtcNow;
                var hoje = DateOnly.FromDateTime(agora);

                using var scope = _scopeFactory.CreateScope();
                var motorService = scope.ServiceProvider.GetRequiredService<IMotorCompraService>();

                if (motorService.IsDataCompra(hoje))
                {
                    _logger.LogInformation("Data de compra detectada: {Data}. Executando motor...", hoje);
                    var resultado = await motorService.ExecutarCompraAsync(hoje, stoppingToken);
                    _logger.LogInformation("Motor executado. Clientes: {N}, Total: R$ {V}",
                        resultado.TotalClientes, resultado.TotalConsolidado);
                }

                var proximaExecucao = new DateTime(agora.Year, agora.Month, agora.Day, 10, 0, 0, DateTimeKind.Utc)
                    .AddDays(1);
                var delay = proximaExecucao - agora;

                if (delay.TotalMinutes > 1)
                    await Task.Delay(delay, stoppingToken);
                else
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no Motor Scheduler");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
