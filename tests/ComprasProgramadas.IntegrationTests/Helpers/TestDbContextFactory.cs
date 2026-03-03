using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.IntegrationTests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string dbName = "")
    {
        if (string.IsNullOrEmpty(dbName))
            dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
