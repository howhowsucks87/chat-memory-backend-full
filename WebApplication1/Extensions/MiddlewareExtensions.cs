using ChatMemoryApi.Middleware;

namespace ChatMemoryApi.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder
        UseGlobalException(
            this IApplicationBuilder app)
    {
        return app.UseMiddleware
            <GlobalExceptionMiddleware>();
    }
}