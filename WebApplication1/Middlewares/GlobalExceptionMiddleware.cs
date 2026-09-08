using System.Text.Json;
using ChatMemoryApi.DTOs;

namespace ChatMemoryApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            context.Response.StatusCode = 500;

            context.Response.ContentType =
                "application/json";

            var response = new ErrorResponse
            {
                Success = false,
                Message = "Internal Server Error"
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}