using CryptoClients.Net;
using CryptoExchange.Net.Objects;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Accounts;
using OrderService.Domain.Users;
using OrderService.Infrastructure.Persistence;

namespace MigrationService.Initializers;

public class ApplicationDbContextInitializer(AppDbContext dbContext) : DbContextInitializerBase<AppDbContext>(dbContext)
{
    public async Task SeedDataAsync(CancellationToken cancellationToken)
    {
        var strategy = DbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            var user = await SeedUser();
            await SeedAccount(user);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task<User> SeedUser()
    {
        if (DbContext.Users.Any())
            return await DbContext.Users.FirstAsync();

        var user = User.Create(
            externalId: "5c182474-f5a4-479e-9bab-f68b9508aaa8",
            email: "warrolen@gmail.com");

        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    private async Task SeedAccount(User user)
    {
        if (DbContext.Accounts.Any())
            return;

        var liveAccount = Account.Create(
            userId: user.Id,
            environment: TradeEnvironmentNames.Live,
            exchange: Exchanges.Binance.Name,
            apiKey: "0lSTI6OYIbb7kz7om36RDkfEX67b8TlZOhJjFjmDRFxdUmbaNDG1jHvqni8F9GKH",
            apiSecret: "Nle0Drk60hVTHUNOsI4U83KBRIbYRENOdCpdPy2QAYDaLJ0I1mgNRL3G0YnteBUw",
            passphrase: null);

        await DbContext.Accounts.AddAsync(liveAccount);
        await DbContext.SaveChangesAsync();
    }
}
