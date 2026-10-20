using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Accounts;
using OrderService.Domain.ExchangeLogs;
using OrderService.Domain.Orders;
using OrderService.Domain.Symbols;
using OrderService.Domain.Users;

namespace OrderService.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Order> Orders { get; }
    DbSet<ExchangeLog> ExchangeLogs { get; }
    DbSet<ExchangeSymbol> ExchangeSymbols { get; }
    DbSet<ExchangeAsset> ExchangeAssets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
