using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Authentication;

namespace OrderService.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated()
    {
        return httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    }

    public Guid? GetUserId()
    {
        return httpContextAccessor.HttpContext?.User.GetUserId();
    }

    public Guid GetUserIdOrThrow()
    {
        return GetUserId() ?? throw new UnauthorizedAccessException("UserId claim is missing");
    }

    public string? GetEmail()
    {
        return httpContextAccessor.HttpContext?.User.GetEmail();
    }
}
