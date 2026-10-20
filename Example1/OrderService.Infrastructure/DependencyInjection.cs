using System.Threading.Channels;
using BuildingBlocks.Jobs;
using BuildingBlocks.Jobs.Common;
using EntityFramework.Exceptions.SqlServer;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.SqlServer;
using Hangfire.Tags;
using Hangfire.Tags.SqlServer;
using Hangfire.Throttling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Authentication;
using OrderService.Infrastructure.PartnerIntegrations;
using CryptoClients.Net;
using CryptoClients.Net.Interfaces;
using OrderService.Infrastructure.PartnerIntegrations.Models;
using OrderService.Infrastructure.PartnerIntegrations.Processors;
using OrderService.Infrastructure.PartnerIntegrations.Publishers;
using OrderService.Infrastructure.PartnerIntegrations.Subscriptions;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Interceptors;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Security;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSecurityInternal();
            services.AddDatabaseInternal(configuration);
            services.AddAuthenticationInternal();
            services.AddPartnerIntegrationsInternal();
            services.AddSubscriptionsInternal();
            services.AddSymbolSyncInternal();
            services.AddHangfireInternal(configuration);

            return services;
        }

        private IServiceCollection AddSecurityInternal()
        {
            services
                .AddOptions<SecurityOptions>()
                .BindConfiguration(SecurityOptions.SectionName)
                .Validate(options => !string.IsNullOrWhiteSpace(options.EncryptionKey),
                    "Security:EncryptionKey must be configured")
                .ValidateOnStart();

            services.AddSingleton<IEncryptionService, EncryptionService>();

            return services;
        }
        
        private IServiceCollection AddDatabaseInternal(IConfiguration configuration)
        {
            services.AddScoped<ISaveChangesInterceptor, EntitySaveChangesInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
            var connectionString = configuration.GetConnectionString("Database");
        
            if (string.IsNullOrEmpty(connectionString)) 
                throw new InvalidOperationException("Database connection string is not configured");
        
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                // Return strongly typed useful exceptions
                options.UseExceptionProcessor();
            });

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }

        private IServiceCollection AddAuthenticationInternal()
        {
            services
                .AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .Validate(options =>
                        !string.IsNullOrWhiteSpace(options.Key) &&
                        !string.IsNullOrWhiteSpace(options.Issuer) &&
                        !string.IsNullOrWhiteSpace(options.Audience),
                    "JWT options must be configured")
                .ValidateOnStart();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            services.ConfigureOptions<ConfigureJwtBearerOptions>();

            services.AddAuthorization();

            return services;
        }

        private IServiceCollection AddPartnerIntegrationsInternal()
        {
            foreach (var exchange in Exchanges.All)
            {
                var name = exchange.Name;
                services.AddScoped<IPartnerIntegration>(sp =>
                    new GenericPartnerIntegration(
                        name,
                        sp.GetRequiredService<IExchangeUserClientProvider>(),
                        sp.GetRequiredService<IExchangeLogRepository>()));
            }

            services.AddScoped<IExchangeLogRepository, ExchangeLogRepository>();

            services.AddSingleton<IExchangeInfoService, ExchangeInfoService>();
            services.AddScoped<IExchangeFeeService, ExchangeFeeService>();

            return services;
        }

        private IServiceCollection AddSubscriptionsInternal()
        {
            // Registers IExchangeUserClientProvider, IExchangeTrackerFactory and all
            // per-exchange services (Binance, Bybit, …) from CryptoClients.Net.
            services.AddCryptoClients(options =>
            {
                options.CachingEnabled = false;
                options.OutputOriginalData = true;
            });

            // Channels — created once; Writer → IPublishOutput<T>, Reader → IProcessInput<T>.
            var orderChannel = Channel.CreateBounded<PublishItem<OrderEvent>>(
                new BoundedChannelOptions(capacity: 1_000)
                {
                    FullMode     = BoundedChannelFullMode.Wait,
                    SingleWriter = false,
                    SingleReader = true
                });
            services.AddSingleton<IPublishOutput<OrderEvent>>(new ChannelPublishOutput<OrderEvent>(orderChannel.Writer));
            services.AddSingleton<IProcessInput<OrderEvent>>(new ChannelProcessInput<OrderEvent>(orderChannel.Reader));

            var balanceChannel = Channel.CreateUnbounded<PublishItem<BalanceEvent>>(
                new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
            services.AddSingleton<IPublishOutput<BalanceEvent>>(new ChannelPublishOutput<BalanceEvent>(balanceChannel.Writer));
            services.AddSingleton<IProcessInput<BalanceEvent>>(new ChannelProcessInput<BalanceEvent>(balanceChannel.Reader));

            var tradeChannel = Channel.CreateUnbounded<PublishItem<TradeEvent>>(
                new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
            services.AddSingleton<IPublishOutput<TradeEvent>>(new ChannelPublishOutput<TradeEvent>(tradeChannel.Writer));
            services.AddSingleton<IProcessInput<TradeEvent>>(new ChannelProcessInput<TradeEvent>(tradeChannel.Reader));

            // Pure subscription manager.
            services.AddSingleton<UserSubscriptionService>();

            // Queue for notifying the worker of accounts added after startup.
            services.AddSingleton<AccountSubscriptionQueue>();

            // Seeds the queue on startup, then drains it for runtime additions.
            services.AddHostedService<AccountSubscriptionWorker>();

            // Per-stream processors.
            services.AddHostedService<OrderUpdateProcessor>();
            services.AddHostedService<BalanceUpdateProcessor>();
            services.AddHostedService<TradeUpdateProcessor>();

            return services;
        }

        private IServiceCollection AddSymbolSyncInternal()
        {
            var symbolChannel = Channel.CreateBounded<PublishItem<SymbolData>>(
                new BoundedChannelOptions(capacity: 100)
                {
                    FullMode     = BoundedChannelFullMode.DropOldest,
                    SingleWriter = false,
                    SingleReader = true
                });
            services.AddSingleton<IPublishOutput<SymbolData>>(new ChannelPublishOutput<SymbolData>(symbolChannel.Writer));
            services.AddSingleton<IProcessInput<SymbolData>>(new ChannelProcessInput<SymbolData>(symbolChannel.Reader));

            var assetSyncChannel = Channel.CreateBounded<PublishItem<PendingAssetCalculation>>(
                new BoundedChannelOptions(capacity: 10_000)
                {
                    FullMode     = BoundedChannelFullMode.DropOldest,
                    SingleWriter = false,
                    SingleReader = true
                });
            services.AddSingleton<IPublishOutput<PendingAssetCalculation>>(new ChannelPublishOutput<PendingAssetCalculation>(assetSyncChannel.Writer));
            services.AddSingleton<IProcessInput<PendingAssetCalculation>>(new ChannelProcessInput<PendingAssetCalculation>(assetSyncChannel.Reader));

            var tickerChannel = Channel.CreateBounded<PublishItem<TickerData>>(
                new BoundedChannelOptions(capacity: 500)
                {
                    FullMode     = BoundedChannelFullMode.DropOldest,
                    SingleWriter = false,
                    SingleReader = true
                });
            services.AddSingleton<IPublishOutput<TickerData>>(new ChannelPublishOutput<TickerData>(tickerChannel.Writer));
            services.AddSingleton<IProcessInput<TickerData>>(new ChannelProcessInput<TickerData>(tickerChannel.Reader));

            var fiatPriceChannel = Channel.CreateBounded<PublishItem<FiatPriceData>>(
                new BoundedChannelOptions(capacity: 100)
                {
                    FullMode     = BoundedChannelFullMode.DropOldest,
                    SingleWriter = false,
                    SingleReader = true
                });
            services.AddSingleton<IPublishOutput<FiatPriceData>>(new ChannelPublishOutput<FiatPriceData>(fiatPriceChannel.Writer));
            services.AddSingleton<IProcessInput<FiatPriceData>>(new ChannelProcessInput<FiatPriceData>(fiatPriceChannel.Reader));

            services.AddSingleton<TickerSubscriptionService>();
            services.AddHostedService<TickerSubscriptionWorker>();

            services.AddHttpClient<FiatPricePublishService>();
            services.AddHostedService<SymbolPublishService>();
            services.AddHostedService<TickerPublishService>();
            services.AddHostedService<FiatPricePublishService>();
            services.AddHostedService<SymbolsProcessService>();
            services.AddHostedService<TickerProcessService>();
            services.AddHostedService<FiatPricesProcessService>();
            services.AddHostedService<AssetCalculationProcessService>();
            services.AddHostedService<SymbolUsdVolumeService>();

            return services;
        }
    }

    private static IServiceCollection AddHangfireInternal(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrEmpty(connectionString)) 
            throw new InvalidOperationException("Hangfire connection string is not configured");

        var storageOptions = new SqlServerStorageOptions { SchemaName = SchemaNames.Hangfire };
        
        services.AddHangfire(globalConfiguration => globalConfiguration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, storageOptions)
            .UseBatches()
            .UseThrottling(ThrottlingAction.RetryJob, TimeSpan.FromSeconds(1))
            .UseTagsWithSql(new TagsOptions { TagsListStyle = TagsListStyle.Dropdown }, storageOptions));
        
        services.AddHangfireServer((sp, options) =>
        {
            options.Activator = new AspNetCoreJobActivator(sp.GetRequiredService<IServiceScopeFactory>());
            options.WorkerCount = Environment.ProcessorCount * 10;
            options.HeartbeatInterval = TimeSpan.FromSeconds(10);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
            options.Queues = ["critical", "default"];
            options.ServerName = Environment.MachineName;
        });

        services.AddSingleton<IJobRunner, JobRunner>();
        
        return services;
    }
}
