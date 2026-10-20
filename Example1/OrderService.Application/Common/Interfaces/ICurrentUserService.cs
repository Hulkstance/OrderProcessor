namespace OrderService.Application.Common.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated();
    Guid? GetUserId();
    Guid GetUserIdOrThrow();
    string? GetEmail();
}
