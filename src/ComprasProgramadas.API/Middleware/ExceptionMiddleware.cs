using System.Net;
using System.Text.Json;

namespace ComprasProgramadas.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message, "RECURSO_NAO_ENCONTRADO");
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message, "OPERACAO_INVALIDA");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno. Tente novamente.", "ERRO_INTERNO");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string code)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { erro = message, codigo = code };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
