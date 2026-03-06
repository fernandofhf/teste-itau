using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Application.Services.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ComprasProgramadas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IMotorCompraService, MotorCompraService>();
        services.AddScoped<IRebalanceamentoService, RebalanceamentoService>();
        return services;
    }
}
