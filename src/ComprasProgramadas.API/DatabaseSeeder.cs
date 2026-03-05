using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.API;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations aplicadas com sucesso.");

            // Criar conta master se não existir
            if (!await context.ContasGraficas.AnyAsync(c => c.Tipo == TipoConta.Master))
            {
                var master = ContaGrafica.CriarMaster();
                context.ContasGraficas.Add(master);
                await context.SaveChangesAsync();
                logger.LogInformation("Conta Master criada: {NumConta}", master.NumeroConta);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrations ou seeder");
        }
    }
}
