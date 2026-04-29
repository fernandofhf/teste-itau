using ComprasProgramadas.Application.Services.Interfaces;
using Cronos;

namespace ComprasProgramadas.API.BackgroundServices;

public class MotorCompraScheduler : BackgroundService
{
    private static readonly CronExpression _cron = CronExpression.Parse("0 10 * * 1-5");

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
            var agora = DateTime.UtcNow;
            var proxima = _cron.GetNextOccurrence(agora, TimeZoneInfo.Utc);

            if (proxima is null) break;

            var delay = proxima.Value - agora;
            _logger.LogInformation("Próxima verificação agendada para {Proxima} UTC (em {Minutos:F0} min)",
                proxima.Value, delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) { break; }

            try
            {
                var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

                using var scope = _scopeFactory.CreateScope();
                var motorService = scope.ServiceProvider.GetRequiredService<IMotorCompraService>();

                if (motorService.IsDataCompra(hoje))
                {
                    _logger.LogInformation("Data de compra detectada: {Data}. Executando motor...", hoje);
                    var resultado = await motorService.ExecutarCompraAsync(hoje, stoppingToken);
                    _logger.LogInformation("Motor executado. Clientes: {N}, Total: R$ {V}",
                        resultado.TotalClientes, resultado.TotalConsolidado);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no Motor Scheduler");
            }
        }
    }
}
