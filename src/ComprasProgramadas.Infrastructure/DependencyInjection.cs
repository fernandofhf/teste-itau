using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Cotacoes;
using ComprasProgramadas.Infrastructure.Messaging;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using ComprasProgramadas.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ComprasProgramadas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mySql => mySql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IContaGraficaRepository, ContaGraficaRepository>();
        services.AddScoped<ICustodiaRepository, CustodiaRepository>();
        services.AddScoped<ICestaRecomendacaoRepository, CestaRecomendacaoRepository>();
        services.AddScoped<IOrdemCompraRepository, OrdemCompraRepository>();
        services.AddScoped<IDistribuicaoRepository, DistribuicaoRepository>();
        services.AddScoped<IEventoIRRepository, EventoIRRepository>();
        services.AddScoped<ICotacaoRepository, CotacaoRepository>();
        services.AddScoped<IRebalanceamentoRepository, RebalanceamentoRepository>();
        services.AddScoped<IHistoricoAporteRepository, HistoricoAporteRepository>();

        services.AddSingleton<CotahistParser>();
        services.AddSingleton<ICotahistService, CotahistService>();
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

        return services;
    }
}
