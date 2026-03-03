using ComprasProgramadas.API.BackgroundServices;
using ComprasProgramadas.API.Middleware;
using ComprasProgramadas.Application;
using ComprasProgramadas.Infrastructure;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Compras Programadas API",
        Version = "v1",
        Description = "Sistema de Compra Programada de Ações — Itaú Corretora. Permite que clientes adiram a um plano de investimento recorrente em uma carteira Top Five de 5 ações.",
        Contact = new OpenApiContact { Name = "Itaú Corretora — Engenharia de Software" }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Background Scheduler
builder.Services.AddHostedService<MotorCompraScheduler>();

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compras Programadas API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    await ComprasProgramadas.API.DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program { }
