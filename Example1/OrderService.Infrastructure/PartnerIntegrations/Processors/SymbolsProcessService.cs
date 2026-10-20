using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Symbols;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.PartnerIntegrations.Processors;

internal sealed class SymbolsProcessService : BackgroundService
{
    private CancellationToken _stoppingToken = default;
    private readonly ILogger _logger;
    private readonly string[] _usdStableAssets;
    private readonly string[] _fiatAssets;
    private readonly string[] _leveragedTokens;
    private readonly IProcessInput<SymbolData> _processInput;
    private readonly IPublishOutput<PendingAssetCalculation> _assetCalcOutput;
    private readonly IServiceScopeFactory _dbContextFactory;

    public SymbolsProcessService(
        IConfiguration configuration,
        ILogger<SymbolsProcessService> logger,
        IProcessInput<SymbolData> processInput,
        IPublishOutput<PendingAssetCalculation> assetCalcOutput,
        IServiceScopeFactory dbContextFactory)
    {
        _logger = logger;
        _processInput = processInput;
        _assetCalcOutput = assetCalcOutput;
        _dbContextFactory = dbContextFactory;

        _usdStableAssets = configuration.GetValue<string>("UsdStableAssets")!.Split(";");
        _fiatAssets = configuration.GetValue<string>("FiatAssets")!.Split(";");
        _leveragedTokens = configuration.GetValue<string>("LeveragedTokens")!.Split(";");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _logger.LogDebug("Starting SymbolsService");
        while (!_stoppingToken.IsCancellationRequested)
        {
            var input = await _processInput.ReadAsync(stoppingToken);
            if (input != null)
                await ProcessAsync(input);
        }

        _logger.LogDebug("SymbolsService stopped");
    }

    private async Task ProcessAsync(PublishItem<SymbolData> update)
    {
        await using var scope = _dbContextFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get the names of all symbols for the exchange currently in the DB so we know which ones are no longer returned
        var exchangeSymbols = await context.Set<ExchangeSymbol>()
            .Where(x => x.Exchange == update.Exchange && x.DeletedAt == null)
            .Select(x => x.Id)
            .ToHashSetAsync();

        var symbols = new List<ExchangeSymbol>();
        foreach (var item in update.Data)
        {
            var id = $"{update.Exchange}-{item.BaseAsset.ToUpperInvariant()}-{item.QuoteAsset.ToUpperInvariant()}";
            var symbol = ExchangeSymbol.Create(
                exchange:                update.Exchange!,
                baseAsset:               item.BaseAsset.ToUpperInvariant(),
                quoteAsset:              item.QuoteAsset.ToUpperInvariant(),
                baseAssetType:           GetAssetType(item.BaseAsset, true),
                quoteAssetType:          GetAssetType(item.QuoteAsset, false),
                name:                    item.Name,
                minTradeQuantity:        item.MinTradeQuantity,
                minNotionalValue:        item.MinNotionalValue,
                quantityStep:            item.QuantityStep,
                priceStep:               item.PriceStep,
                quantityDecimals:        item.QuantityDecimals,
                priceDecimals:           item.PriceDecimals,
                priceSignificantFigures: item.PriceSignificantFigures,
                enabled:                 item.Trading);

            symbols.Add(symbol);
            exchangeSymbols.Remove(id);
        }

        // Any symbols no longer returned should be set to deleted in DB
        var symbolsToRemove = new List<ExchangeSymbol>();
        foreach (var item in exchangeSymbols)
        {
            symbolsToRemove.Add(ExchangeSymbol.ForRemoval(item, update.Exchange!));
        }

        try
        {
            await context.BulkInsertOrUpdateAsync(symbols, new BulkConfig
            {
                PropertiesToInclude =
                [
                    nameof(ExchangeSymbol.Id),
                    nameof(ExchangeSymbol.Exchange),
                    nameof(ExchangeSymbol.Name),
                    nameof(ExchangeSymbol.BaseAsset),
                    nameof(ExchangeSymbol.BaseAssetExchangeId),
                    nameof(ExchangeSymbol.QuoteAsset),
                    nameof(ExchangeSymbol.QuoteAssetExchangeId),
                    nameof(ExchangeSymbol.MinNotionalValue),
                    nameof(ExchangeSymbol.MinTradeQuantity),
                    nameof(ExchangeSymbol.PriceDecimals),
                    nameof(ExchangeSymbol.PriceSignificantFigures),
                    nameof(ExchangeSymbol.QuantityDecimals),
                    nameof(ExchangeSymbol.QuantityStep),
                    nameof(ExchangeSymbol.BaseAssetType),
                    nameof(ExchangeSymbol.QuoteAssetType),
                    nameof(ExchangeSymbol.Enabled),
                    nameof(ExchangeSymbol.LastSyncedAt),
                    nameof(ExchangeSymbol.DeletedAt)
                ],
                WithHoldlock = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process updated symbols in Symbol update");
        }

        if (symbolsToRemove.Count > 0)
        {
            try
            {
                await context.BulkInsertOrUpdateAsync(symbolsToRemove, new BulkConfig
                {
                    PropertiesToInclude =
                    [
                        nameof(ExchangeSymbol.Id),
                        nameof(ExchangeSymbol.Exchange),
                        nameof(ExchangeSymbol.LastPrice),
                        nameof(ExchangeSymbol.HighPrice),
                        nameof(ExchangeSymbol.LowPrice),
                        nameof(ExchangeSymbol.Volume),
                        nameof(ExchangeSymbol.QuoteVolume),
                        nameof(ExchangeSymbol.ChangePercentage),
                        nameof(ExchangeSymbol.Enabled),
                        nameof(ExchangeSymbol.LastSyncedAt),
                        nameof(ExchangeSymbol.DeletedAt)
                    ],
                    WithHoldlock = false
                });

                // When a symbol is removed the asset stats should be recalculated
                await _assetCalcOutput.PublishAsync(new PublishItem<PendingAssetCalculation>
                {
                    Data = symbolsToRemove.Select(x => new PendingAssetCalculation { Exchange = x.Exchange, Asset = x.Id.Split('-')[1] })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process removed symbols in Symbol update");
            }
        }
    }

    private AssetType GetAssetType(string asset, bool checkLeveraged)
    {
        if (_usdStableAssets.Contains(asset))
            return AssetType.Stable;

        if (_fiatAssets.Contains(asset))
            return AssetType.Fiat;

        if (checkLeveraged && _leveragedTokens.Any(x => asset.Contains(x)))
            return AssetType.LeveragedToken;

        return AssetType.Crypto;
    }
}
