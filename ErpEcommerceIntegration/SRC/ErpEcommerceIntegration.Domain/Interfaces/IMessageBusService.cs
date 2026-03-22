using ErpEcommerceIntegration.Domain.Models;

namespace ErpEcommerceIntegration.Domain.Interfaces;

public interface IMessageBusService
{
    Task PublishProductSyncAsync(ProductSync product, CancellationToken ct = default);
    Task PublishInventorySyncAsync(InventorySync inventory, CancellationToken ct = default);
    Task PublishColorSyncAsync(ColorSync color, CancellationToken ct = default);
    Task PublishSizeSyncAsync(SizeSync size, CancellationToken ct = default);
}
