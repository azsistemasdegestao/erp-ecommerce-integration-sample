using ErpEcommerceIntegration.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.Sync;

public sealed class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IERPDatabaseService _erp;
    private readonly IMessageBusService _bus;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(IERPDatabaseService erp, IMessageBusService bus, ILogger<SyncOrchestrator> logger)
    {
        _erp = erp;
        _bus = bus;
        _logger = logger;
    }

    public async Task SynchronizeProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SYNC] SynchronizeProducts started");

        var products = (await _erp.GetProductsAsync(ct: ct)).ToList();
        _logger.LogInformation("[SYNC] Fetched {Count} products from ERP", products.Count);

        var published = 0;
        foreach (var product in products.Where(p => p.IsActive))
        {
            await _bus.PublishProductSyncAsync(product, ct);
            published++;
        }

        _logger.LogInformation("[SYNC] SynchronizeProducts completed — Published={Published} Skipped={Skipped}",
            published, products.Count - published);
    }

    public async Task SynchronizeInventoryAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SYNC] SynchronizeInventory started");
        var items = (await _erp.GetInventoryAsync(ct: ct)).ToList();
        foreach (var item in items)
            await _bus.PublishInventorySyncAsync(item, ct);
        _logger.LogInformation("[SYNC] SynchronizeInventory completed — Published={Count}", items.Count);
    }

    public async Task SynchronizeColorsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SYNC] SynchronizeColors started");
        var colors = (await _erp.GetColorsAsync(ct)).ToList();
        foreach (var color in colors)
            await _bus.PublishColorSyncAsync(color, ct);
        _logger.LogInformation("[SYNC] SynchronizeColors completed — Published={Count}", colors.Count);
    }

    public async Task SynchronizeSizesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SYNC] SynchronizeSizes started");
        var sizes = (await _erp.GetSizesAsync(ct)).ToList();
        foreach (var size in sizes)
            await _bus.PublishSizeSyncAsync(size, ct);
        _logger.LogInformation("[SYNC] SynchronizeSizes completed — Published={Count}", sizes.Count);
    }
}
