using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using ErpEcommerceIntegration.Domain.Models;
using ErpEcommerceIntegration.Infra.MessageBus.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ErpEcommerceIntegration.IntegrationTests.Consumers;

/// <summary>
/// Integration tests for MassTransit consumers.
/// Uses MassTransit's built-in ITestHarness (AddMassTransitTestHarness) so the full
/// publish → consume pipeline runs in-memory, including retry middleware if configured.
/// IECommerceEndPoint is replaced by an NSubstitute mock to control success/failure paths.
/// </summary>
public sealed class SyncConsumersTests : IAsyncLifetime
{
    private readonly IECommerceEndPoint _eCommerceMock = Substitute.For<IECommerceEndPoint>();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton(_eCommerceMock)
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

    // ── SyncProductsConsumer ─────────────────────────────────────────────────

    [Fact]
    public async Task SyncProductsConsumer_WhenUpsertSucceeds_MessageIsConsumedWithoutFault()
    {
        var product = MakeProduct("SKU-TEST-001");
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(true);

        await _harness.Bus.Publish(new ProductSyncMessage(product));

        Assert.True(await _harness.Consumed.Any<ProductSyncMessage>());
        Assert.False(await _harness.Published.Any<Fault<ProductSyncMessage>>());
    }

    [Fact]
    public async Task SyncProductsConsumer_WhenUpsertReturnsFalse_MessageFaults()
    {
        var product = MakeProduct("SKU-REJECT");
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(false);

        await _harness.Bus.Publish(new ProductSyncMessage(product));

        Assert.True(await _harness.Consumed.Any<ProductSyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<ProductSyncMessage>>());
    }

    [Fact]
    public async Task SyncProductsConsumer_WhenUpsertThrows_MessageFaults()
    {
        var product = MakeProduct("SKU-THROW");
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .ThrowsAsync(new HttpRequestException("Simulated network error"));

        await _harness.Bus.Publish(new ProductSyncMessage(product));

        Assert.True(await _harness.Consumed.Any<ProductSyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<ProductSyncMessage>>());
    }

    [Fact]
    public async Task SyncProductsConsumer_CallsUpsertWithCorrectSku()
    {
        var product = MakeProduct("SKU-VERIFY");
        _eCommerceMock.UpsertProductAsync(Arg.Any<ProductSync>(), Arg.Any<CancellationToken>())
                      .Returns(true);

        await _harness.Bus.Publish(new ProductSyncMessage(product));

        await _harness.Consumed.Any<ProductSyncMessage>();
        await _eCommerceMock.Received(1).UpsertProductAsync(
            Arg.Is<ProductSync>(p => p.Sku == "SKU-VERIFY"),
            Arg.Any<CancellationToken>());
    }

    // ── SyncInventoryConsumer ────────────────────────────────────────────────

    [Fact]
    public async Task SyncInventoryConsumer_WhenUpdateSucceeds_MessageIsConsumedWithoutFault()
    {
        var inventory = new InventorySync { Sku = "SKU-INV", QuantityAvailable = 50, WarehouseCode = "WH-A" };
        _eCommerceMock.UpdateInventoryAsync(Arg.Any<InventorySync>(), Arg.Any<CancellationToken>())
                      .Returns(true);

        await _harness.Bus.Publish(new InventorySyncMessage(inventory));

        Assert.True(await _harness.Consumed.Any<InventorySyncMessage>());
        Assert.False(await _harness.Published.Any<Fault<InventorySyncMessage>>());
    }

    [Fact]
    public async Task SyncInventoryConsumer_WhenUpdateReturnsFalse_MessageFaults()
    {
        var inventory = new InventorySync { Sku = "SKU-INV-FAIL", QuantityAvailable = 5, WarehouseCode = "WH-B" };
        _eCommerceMock.UpdateInventoryAsync(Arg.Any<InventorySync>(), Arg.Any<CancellationToken>())
                      .Returns(false);

        await _harness.Bus.Publish(new InventorySyncMessage(inventory));

        Assert.True(await _harness.Consumed.Any<InventorySyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<InventorySyncMessage>>());
    }

    // ── SyncColorsConsumer ───────────────────────────────────────────────────

    [Fact]
    public async Task SyncColorsConsumer_WhenUpsertSucceeds_MessageIsConsumedWithoutFault()
    {
        var color = new ColorSync { Id = 1, Code = "GRN", Label = "Green" };
        _eCommerceMock.UpsertColorAsync(Arg.Any<ColorSync>(), Arg.Any<CancellationToken>())
                      .Returns(true);

        await _harness.Bus.Publish(new ColorSyncMessage(color));

        Assert.True(await _harness.Consumed.Any<ColorSyncMessage>());
        Assert.False(await _harness.Published.Any<Fault<ColorSyncMessage>>());
    }

    [Fact]
    public async Task SyncColorsConsumer_WhenUpsertReturnsFalse_MessageFaults()
    {
        var color = new ColorSync { Id = 99, Code = "BAD", Label = "Bad" };
        _eCommerceMock.UpsertColorAsync(Arg.Any<ColorSync>(), Arg.Any<CancellationToken>())
                      .Returns(false);

        await _harness.Bus.Publish(new ColorSyncMessage(color));

        Assert.True(await _harness.Consumed.Any<ColorSyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<ColorSyncMessage>>());
    }

    // ── SyncSizesConsumer ────────────────────────────────────────────────────

    [Fact]
    public async Task SyncSizesConsumer_WhenUpsertSucceeds_MessageIsConsumedWithoutFault()
    {
        var size = new SizeSync { Id = 1, Code = "XXL", Label = "Double XL" };
        _eCommerceMock.UpsertSizeAsync(Arg.Any<SizeSync>(), Arg.Any<CancellationToken>())
                      .Returns(true);

        await _harness.Bus.Publish(new SizeSyncMessage(size));

        Assert.True(await _harness.Consumed.Any<SizeSyncMessage>());
        Assert.False(await _harness.Published.Any<Fault<SizeSyncMessage>>());
    }

    [Fact]
    public async Task SyncSizesConsumer_WhenUpsertReturnsFalse_MessageFaults()
    {
        var size = new SizeSync { Id = 99, Code = "???", Label = "Unknown" };
        _eCommerceMock.UpsertSizeAsync(Arg.Any<SizeSync>(), Arg.Any<CancellationToken>())
                      .Returns(false);

        await _harness.Bus.Publish(new SizeSyncMessage(size));

        Assert.True(await _harness.Consumed.Any<SizeSyncMessage>());
        Assert.True(await _harness.Published.Any<Fault<SizeSyncMessage>>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProductSync MakeProduct(string sku) =>
        new() { Sku = sku, Name = $"Test {sku}", Price = 99.9m, IsActive = true, UpdatedAtERP = DateTime.UtcNow };
}
