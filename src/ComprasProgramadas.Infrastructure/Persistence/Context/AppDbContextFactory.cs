using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ComprasProgramadas.Infrastructure.Persistence.Context;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Database=compras_programadas;User=root;Password=root;",
            new MySqlServerVersion(new Version(8, 0, 0)));

        return new AppDbContext(optionsBuilder.Options);
    }
}
