using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using ErpEcommerceIntegration.Domain.Models;
using ErpEcommerceIntegration.Infra.ERPDatabase;
using ErpEcommerceIntegration.Infra.MessageBus;
using ErpEcommerceIntegration.Infra.MessageBus.Consumers;
using ErpEcommerceIntegration.Infra.Sync;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Diagnostics;

namespace ErpEcommerceIntegration.IntegrationTests.Pipeline;

/// <summary>
/// End-to-end integration tests for the full ERP → Bus → Consumer → E-Commerce pipeline.
///
/// These tests wire together real implementations of SyncOrchestrator, MessageBusService,
/// and all MassTransit consumers. Only IECommerceEndPoint is mocked to keep tests
/// deterministic (the real implementation has ~5% random failures).
///
/// This validates that:
/// - The DI graph resolves correctly
/// - Messages are routed to the correct consumers
/// - The consumers call the e-commerce endpoint with the expected data
/// </summary>
public sealed class FullPipelineTests : IAsyncLifetime
{
    private readonly IECommerceEndPoint _eCommerceMock = Substitute.For<IECommerceEndPoint>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MessageBus:UseInMemory"] = "true" })
            .Build();

        _provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton(_eCommerceMock)
            .AddScoped<IERPDatabaseService, ERPDatabaseService>()
            .AddScoped<IMessageBusService, MessageBusService>()
            .AddScoped<ISyncOrchestrator, SyncOrchestrator>()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SyncProductsConsumer>();
                x.AddConsumer<SyncInventoryConsumer>();
                x.AddConsumer<SyncColorsConsumer>();
                x.AddConsumer<SyncSizesConsumer>();
            })
            .AddLogging()
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    // ── Product pipeline ─────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_ProductSync_PublishesActiveProductsAndConsumerCallsECommerce()
    {
        // Use an atomic counter in the callback so we can reliably wait for all
        // consumer invocations to complete without depending on NSubstitute internals.
        var count = 0;
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref count); return Task.FromResult(true); });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();
        await orchestrator.SynchronizeProductsAsync();

        // The mock ERP has 4 active products; wait until all 4 are processed
        await WaitForCountAsync(() => count, expectedCount: 4);
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task FullPipeline_ProductSync_InactiveProductsAreNotPublished()
    {
        var skusUpserted = new System.Collections.Concurrent.ConcurrentBag<string>();
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(callInfo =>
                      {
                          skusUpserted.Add(callInfo.Arg<ProductSync>().Sku);
                          return Task.FromResult(true);
                      });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();
        await orchestrator.SynchronizeProductsAsync();

        await WaitForCountAsync(() => skusUpserted.Count, expectedCount: 4);

        // SKU-005 is inactive — it must NOT be sent to the e-commerce platform
        Assert.DoesNotContain("SKU-005", skusUpserted);
    }

    // ── Inventory pipeline ───────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_InventorySync_PublishesAllItemsIncludingZeroStock()
    {
        var count = 0;
        _eCommerceMock.UpdateInventoryAsync(Arg.Any<InventorySync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref count); return Task.FromResult(true); });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();
        await orchestrator.SynchronizeInventoryAsync();

        // All 5 inventory lines are published (including SKU-005 with qty=0)
        await WaitForCountAsync(() => count, expectedCount: 5);
        Assert.Equal(5, count);
    }

    // ── Colors pipeline ──────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_ColorsSync_PublishesAllFourColors()
    {
        var count = 0;
        _eCommerceMock.UpsertColorAsync(Arg.Any<ColorSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref count); return Task.FromResult(true); });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();
        await orchestrator.SynchronizeColorsAsync();

        await WaitForCountAsync(() => count, expectedCount: 4);
        Assert.Equal(4, count);
    }

    // ── Sizes pipeline ───────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_SizesSync_PublishesAllFourSizes()
    {
        var count = 0;
        _eCommerceMock.UpsertSizeAsync(Arg.Any<SizeSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref count); return Task.FromResult(true); });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();
        await orchestrator.SynchronizeSizesAsync();

        await WaitForCountAsync(() => count, expectedCount: 4);
        Assert.Equal(4, count);
    }

    // ── Message routing ──────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_AllSyncTypes_MessagesRoutedToCorrectConsumers()
    {
        var products = 0; var inventory = 0; var colors = 0; var sizes = 0;

        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref products); return Task.FromResult(true); });
        _eCommerceMock.UpdateInventoryAsync(Arg.Any<InventorySync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref inventory); return Task.FromResult(true); });
        _eCommerceMock.UpsertColorAsync(Arg.Any<ColorSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref colors); return Task.FromResult(true); });
        _eCommerceMock.UpsertSizeAsync(Arg.Any<SizeSync>(), Arg.Any<CancellationToken>())
                      .Returns(_ => { Interlocked.Increment(ref sizes); return Task.FromResult(true); });

        await using var scope = _provider.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISyncOrchestrator>();

        await Task.WhenAll(
            orchestrator.SynchronizeProductsAsync(),
            orchestrator.SynchronizeInventoryAsync(),
            orchestrator.SynchronizeColorsAsync(),
            orchestrator.SynchronizeSizesAsync());

        // Wait for all 17 messages (4+5+4+4) to be processed
        await WaitForCountAsync(() => products + inventory + colors + sizes, expectedCount: 17);

        Assert.Equal(4, products);
        Assert.Equal(5, inventory);
        Assert.Equal(4, colors);
        Assert.Equal(4, sizes);

        // No faults should have been published
        Assert.False(await _harness.Published.Any<Fault<ProductSyncMessage>>());
        Assert.False(await _harness.Published.Any<Fault<InventorySyncMessage>>());
        Assert.False(await _harness.Published.Any<Fault<ColorSyncMessage>>());
        Assert.False(await _harness.Published.Any<Fault<SizeSyncMessage>>());
    }

    // ── Transient failures ───────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_WhenECommerceThrowsOnce_ConsumerFaultsMessage()
    {
        // Simulate a transient failure — consumer re-throws, MassTransit publishes a Fault<T>
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await _harness.Bus.Publish(new ProductSyncMessage(
            new ProductSync { Sku = "SKU-FAIL", Name = "Fail Product", Price = 1m, IsActive = true, UpdatedAtERP = DateTime.UtcNow }));

        Assert.True(await _harness.Consumed.Any<ProductSyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<ProductSyncMessage>>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Polls an atomic counter until it reaches <paramref name="expectedCount"/> or
    /// the timeout expires. This is necessary because MassTransit dispatches messages
    /// to consumers asynchronously; the consumer's Consume() may not have finished
    /// by the time the orchestrator's publish loop returns.
    /// The counter is incremented inside the NSubstitute callback, guaranteeing it only
    /// increments after the e-commerce call actually executes (not during mock setup).
    /// </summary>
    private static async Task WaitForCountAsync(
        Func<int> getCount,
        int expectedCount,
        int timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (getCount() >= expectedCount)
                return;
            await Task.Delay(50);
        }
        Assert.Fail(
            $"Timed out after {timeoutMs}ms waiting for {expectedCount} e-commerce calls. " +
            $"Actual: {getCount()}");
    }
}
