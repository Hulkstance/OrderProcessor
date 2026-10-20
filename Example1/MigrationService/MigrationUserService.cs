using OrderService.Application.Common.Interfaces;

namespace MigrationService;

public class MigrationUserService : ICurrentUserService
{
    public bool IsAuthenticated() => true;

    public Guid? GetUserId() => Guid.Parse("ea2d37a7-1434-46e1-819c-40448ed74548");

    public Guid GetUserIdOrThrow() => Guid.Parse("ea2d37a7-1434-46e1-819c-40448ed74548");

    public string GetEmail() => "System";
}
