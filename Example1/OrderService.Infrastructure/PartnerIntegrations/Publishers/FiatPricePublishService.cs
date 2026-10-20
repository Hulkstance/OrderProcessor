using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace OrderService.Infrastructure.PartnerIntegrations.Publishers;

internal sealed class FiatPricePublishService : BackgroundService
{
    private CancellationToken _stoppingToken = default;
    private readonly ILogger _logger;
    private readonly IPublishOutput<FiatPriceData> _publishOutput;
    private readonly HttpClient _httpClient;
    private readonly string _appId;
    private readonly int _pollInterval;

    public FiatPricePublishService(
        ILogger<FiatPricePublishService> logger,
        IConfiguration configuration,
        IPublishOutput<FiatPriceData> publishOutput,
        HttpClient httpClient)
    {
        _logger = logger;
        _publishOutput = publishOutput;
        _httpClient = httpClient;
        _pollInterval = configuration.GetValue<int?>("FiatPricesPollInterval") ?? 1440;
        _appId = configuration["Keys:OpenExchangeRates"] ?? throw new InvalidOperationException("Missing Keys:OpenExchangeRates configuration");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        _logger.LogDebug("Starting FiatPricePublishService");
        while (!_stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            await ProcessAsync();

            var waitTime = TimeSpan.FromMinutes(_pollInterval);
            waitTime = waitTime.Add(-sw.Elapsed);
            if (waitTime < TimeSpan.FromMilliseconds(1))
                waitTime = TimeSpan.FromMilliseconds(1);

            try { await Task.Delay(waitTime, _stoppingToken); } catch { }
        }

        _logger.LogDebug("FiatPricePublishService stopped");
    }

    private async Task ProcessAsync()
    {
        FiatResponse? result = null;
        try
        {
            result = await _httpClient.GetFromJsonAsync<FiatResponse>(
                "https://openexchangerates.org/api/latest.json?" +
                $"app_id={_appId}" +
                "&base=USD" +
                "&prettyprint=false" +
                "&show_alternative=false");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request Fiat prices from API");
            return;
        }

        var exchangeData = new PublishItem<FiatPriceData>(string.Empty);
        var data = new List<FiatPriceData>();
        foreach (var rate in result!.Rates)
        {
            data.Add(new FiatPriceData(rate.Key, rate.Value));
        }

        exchangeData.Data = data;
        await _publishOutput.PublishAsync(exchangeData);
    }

    private sealed class FiatResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
