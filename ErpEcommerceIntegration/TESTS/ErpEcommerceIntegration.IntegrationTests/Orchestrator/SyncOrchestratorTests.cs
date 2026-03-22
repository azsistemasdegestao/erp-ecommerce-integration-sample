using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Models;
using ErpEcommerceIntegration.Infra.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErpEcommerceIntegration.IntegrationTests.Orchestrator;

/// <summary>
/// Integration tests for SyncOrchestrator.
/// Dependencies (IERPDatabaseService, IMessageBusService) are replaced by NSubstitute mocks
/// so these tests focus purely on the orchestrator's coordination logic.
/// </summary>
public sealed class SyncOrchestratorTests
{
    private readonly IERPDatabaseService _erpMock    = Substitute.For<IERPDatabaseService>();
    private readonly IMessageBusService  _busMock    = Substitute.For<IMessageBusService>();
    private readonly SyncOrchestrator    _sut;

    public SyncOrchestratorTests()
    {
        _sut = new SyncOrchestrator(_erpMock, _busMock, NullLogger<SyncOrchestrator>.Instance);
    }

    // ── SynchronizeProducts ──────────────────────────────────────────────────

    [Fact]
    public async Task SynchronizeProductsAsync_PublishesOnlyActiveProducts()
    {
        var products = new[]
        {
            MakeProduct("SKU-A", isActive: true),
            MakeProduct("SKU-B", isActive: false),
            MakeProduct("SKU-C", isActive: true),
        };
        _erpMock.GetProductsAsync(ct: Arg.Any<CancellationToken>()).Returns(products);

        await _sut.SynchronizeProductsAsync();

        await _busMock.Received(1).PublishProductSyncAsync(
            Arg.Is<ProductSync>(p => p.Sku == "SKU-A"), Arg.Any<CancellationToken>());
        await _busMock.Received(1).PublishProductSyncAsync(
            Arg.Is<ProductSync>(p => p.Sku == "SKU-C"), Arg.Any<CancellationToken>());
        await _busMock.DidNotReceive().PublishProductSyncAsync(
            Arg.Is<ProductSync>(p => p.Sku == "SKU-B"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizeProductsAsync_WhenAllInactive_PublishesNothing()
    {
        var products = new[]
        {
            MakeProduct("SKU-X", isActive: false),
            MakeProduct("SKU-Y", isActive: false),
        };
        _erpMock.GetProductsAsync(ct: Arg.Any<CancellationToken>()).Returns(products);

        await _sut.SynchronizeProductsAsync();

        await _busMock.DidNotReceiveWithAnyArgs().PublishProductSyncAsync(default!, default);
    }

    [Fact]
    public async Task SynchronizeProductsAsync_WhenErpReturnsEmpty_PublishesNothing()
    {
        _erpMock.GetProductsAsync(ct: Arg.Any<CancellationToken>()).Returns(Array.Empty<ProductSync>());

        await _sut.SynchronizeProductsAsync();

        await _busMock.DidNotReceiveWithAnyArgs().PublishProductSyncAsync(default!, default);
    }

    [Fact]
    public async Task SynchronizeProductsAsync_PassesCancellationTokenToErp()
    {
        _erpMock.GetProductsAsync(ct: Arg.Any<CancellationToken>()).Returns(Array.Empty<ProductSync>());
        using var cts = new CancellationTokenSource();

        await _sut.SynchronizeProductsAsync(cts.Token);

        await _erpMock.Received(1).GetProductsAsync(ct: cts.Token);
    }

    // ── SynchronizeInventory ─────────────────────────────────────────────────

    [Fact]
    public async Task SynchronizeInventoryAsync_PublishesAllItems()
    {
        var items = new[]
        {
            new InventorySync { Sku = "SKU-1", QuantityAvailable = 10, WarehouseCode = "WH-A" },
            new InventorySync { Sku = "SKU-2", QuantityAvailable = 0,  WarehouseCode = "WH-B" },
        };
        _erpMock.GetInventoryAsync(Arg.Any<string[]?>(), Arg.Any<CancellationToken>()).Returns(items);

        await _sut.SynchronizeInventoryAsync();

        await _busMock.Received(2).PublishInventorySyncAsync(Arg.Any<InventorySync>(), Arg.Any<CancellationToken>());
        await _busMock.Received(1).PublishInventorySyncAsync(
            Arg.Is<InventorySync>(i => i.Sku == "SKU-1"), Arg.Any<CancellationToken>());
        await _busMock.Received(1).PublishInventorySyncAsync(
            Arg.Is<InventorySync>(i => i.Sku == "SKU-2"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizeInventoryAsync_PublishesZeroQuantityItems()
    {
        // Out-of-stock items must still be synced so the e-commerce shows them as unavailable
        var items = new[] { new InventorySync { Sku = "SKU-OOS", QuantityAvailable = 0, WarehouseCode = "WH-A" } };
        _erpMock.GetInventoryAsync(Arg.Any<string[]?>(), Arg.Any<CancellationToken>()).Returns(items);

        await _sut.SynchronizeInventoryAsync();

        await _busMock.Received(1).PublishInventorySyncAsync(
            Arg.Is<InventorySync>(i => i.QuantityAvailable == 0), Arg.Any<CancellationToken>());
    }

    // ── SynchronizeColors ────────────────────────────────────────────────────

    [Fact]
    public async Task SynchronizeColorsAsync_PublishesAllColors()
    {
        var colors = new[]
        {
            new ColorSync { Id = 1, Code = "BLU", Label = "Blue" },
            new ColorSync { Id = 2, Code = "RED", Label = "Red"  },
        };
        _erpMock.GetColorsAsync(Arg.Any<CancellationToken>()).Returns(colors);

        await _sut.SynchronizeColorsAsync();

        await _busMock.Received(2).PublishColorSyncAsync(Arg.Any<ColorSync>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizeColorsAsync_WhenEmpty_PublishesNothing()
    {
        _erpMock.GetColorsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<ColorSync>());

        await _sut.SynchronizeColorsAsync();

        await _busMock.DidNotReceiveWithAnyArgs().PublishColorSyncAsync(default!, default);
    }

    // ── SynchronizeSizes ─────────────────────────────────────────────────────

    [Fact]
    public async Task SynchronizeSizesAsync_PublishesAllSizes()
    {
        var sizes = new[]
        {
            new SizeSync { Id = 1, Code = "S", Label = "Small"  },
            new SizeSync { Id = 2, Code = "M", Label = "Medium" },
            new SizeSync { Id = 3, Code = "L", Label = "Large"  },
        };
        _erpMock.GetSizesAsync(Arg.Any<CancellationToken>()).Returns(sizes);

        await _sut.SynchronizeSizesAsync();

        await _busMock.Received(3).PublishSizeSyncAsync(Arg.Any<SizeSync>(), Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProductSync MakeProduct(string sku, bool isActive) =>
        new() { Sku = sku, Name = $"Product {sku}", Price = 10m, IsActive = isActive, UpdatedAtERP = DateTime.UtcNow };
}
