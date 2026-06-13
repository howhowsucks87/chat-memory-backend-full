using WebApplication1.Middleware;

namespace WebApplication1.Extensions;

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