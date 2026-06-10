using System.Security.Claims;

namespace WebApplication1.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException(
                "UserId claim not found."
            );
        }

        return int.Parse(userId);
    }
}