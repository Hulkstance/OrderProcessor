using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Accounts;
using OrderService.Domain.Common.Interfaces;
using OrderService.Domain.ExchangeLogs;
using OrderService.Domain.Orders;
using OrderService.Domain.Symbols;
using OrderService.Domain.Users;
using OrderService.Infrastructure.Persistence.Extensions;
using OrderService.Infrastructure.Security;
using System.Reflection;

namespace OrderService.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IEncryptionService encryptionService) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => AggregateRootSet<User>();
    public DbSet<Account> Accounts => AggregateRootSet<Account>();
    public DbSet<Order> Orders => AggregateRootSet<Order>();
    public DbSet<ExchangeLog> ExchangeLogs => AggregateRootSet<ExchangeLog>();
    public DbSet<ExchangeSymbol> ExchangeSymbols => Set<ExchangeSymbol>();
    public DbSet<ExchangeAsset> ExchangeAssets => Set<ExchangeAsset>();
    public DbSet<FiatPrice> FiatPrices => Set<FiatPrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        SetEncryption(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void SetEncryption(ModelBuilder modelBuilder)
    {
        var converter = new EncryptedConverter(encryptionService);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Retrieve all string properties that carry the specific encryption metadata annotation
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string) &&
                            p.FindAnnotation(PropertyBuilderExtensions.EncryptionAnnotation) != null);

            foreach (var property in properties)
            {
                // Assigns the value converter to handle transparent encryption/decryption during DB I/O
                property.SetValueConverter(converter);
            }
        }
    }
    
    private DbSet<T> AggregateRootSet<T>() where T : class, IAggregateRoot => Set<T>();
}