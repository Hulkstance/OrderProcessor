using System.Security.Claims;

namespace OrderService.Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public Guid? GetUserId()
        {
            var value = principal.FindFirstValue("userid");
            return Guid.TryParse(value, out var userId) ? userId : null;
        }

        public string? GetEmail()
        {
            return principal.FindFirstValue(ClaimTypes.Email);
        }
    }
}
