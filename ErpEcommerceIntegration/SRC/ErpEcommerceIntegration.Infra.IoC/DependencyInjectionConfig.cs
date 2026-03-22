using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Infra.ERPDatabase;
using ErpEcommerceIntegration.Infra.ECommerce;
using ErpEcommerceIntegration.Infra.MessageBus;
using ErpEcommerceIntegration.Infra.MessageBus.Consumers;
using ErpEcommerceIntegration.Infra.Sync;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ErpEcommerceIntegration.Infra.IoC;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- ERP ---
        services.AddScoped<IERPDatabaseService, ERPDatabaseService>();

        // --- E-Commerce ---
        // In production: replace AddScoped with AddHttpClient<IECommerceEndPoint, ECommerceEndPoint>()
        //   .AddPolicyHandler(GetRetryPolicy()) to get automatic Polly + IHttpClientFactory integration.
        services.AddScoped<IECommerceEndPoint, ECommerceEndPoint>();

        // --- Message Bus ---
        services.AddScoped<IMessageBusService, MessageBusService>();

        // --- Sync ---
        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        // --- Polly Retry (singleton — shared across all scoped services) ---
        services.AddSingleton<AsyncRetryPolicy>(sp =>
            Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (ex, delay, attempt, _) =>
                        sp.GetRequiredService<ILogger<AsyncRetryPolicy>>()
                          .LogWarning(ex, "Retry {Attempt}/3 in {Delay:F1}s — {Message}", attempt, delay.TotalSeconds, ex.Message)));

        // --- MassTransit ---
        var useInMemory = configuration.GetValue<bool>("MessageBus:UseInMemory");
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SyncProductsConsumer>();
            x.AddConsumer<SyncInventoryConsumer>();
            x.AddConsumer<SyncColorsConsumer>();
            x.AddConsumer<SyncSizesConsumer>();

            if (useInMemory)
            {
                // Default for local/demo — no Azure subscription required
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
                // Production: provide real Service Bus connection string in appsettings
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    cfg.Host(configuration["MessageBus:ConnectionString"]);

                    cfg.ReceiveEndpoint("sync-products", e =>
                    {
                        e.ConfigureConsumer<SyncProductsConsumer>(ctx);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(30)));
                        e.UseCircuitBreaker(cb =>
                        {
                            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                            cb.TripThreshold = 15;
                            cb.ActiveThreshold = 10;
                            cb.ResetInterval = TimeSpan.FromMinutes(5);
                        });
                        e.ConcurrentMessageLimit = 4;
                    });

                    cfg.ReceiveEndpoint("sync-inventory", e =>
                    {
                        e.ConfigureConsumer<SyncInventoryConsumer>(ctx);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(30)));
                        e.ConcurrentMessageLimit = 4;
                    });

                    cfg.ReceiveEndpoint("sync-colors", e =>
                    {
                        e.ConfigureConsumer<SyncColorsConsumer>(ctx);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(30)));
                        e.ConcurrentMessageLimit = 1;
                    });

                    cfg.ReceiveEndpoint("sync-sizes", e =>
                    {
                        e.ConfigureConsumer<SyncSizesConsumer>(ctx);
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(30)));
                        e.ConcurrentMessageLimit = 1;
                    });
                });
            }
        });

        return services;
    }
}
