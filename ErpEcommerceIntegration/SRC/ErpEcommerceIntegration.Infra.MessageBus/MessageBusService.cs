using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using ErpEcommerceIntegration.Domain.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.MessageBus;

public sealed class MessageBusService : IMessageBusService
{
    private readonly IBus _bus;
    private readonly ILogger<MessageBusService> _logger;

    public MessageBusService(IBus bus, ILogger<MessageBusService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishProductSyncAsync(ProductSync product, CancellationToken ct = default)
    {
        await _bus.Publish(new ProductSyncMessage(product), ct);
        _logger.LogDebug("[BUS] Published ProductSyncMessage — SKU={Sku}", product.Sku);
    }

    public async Task PublishInventorySyncAsync(InventorySync inventory, CancellationToken ct = default)
    {
        await _bus.Publish(new InventorySyncMessage(inventory), ct);
        _logger.LogDebug("[BUS] Published InventorySyncMessage — SKU={Sku}", inventory.Sku);
    }

    public async Task PublishColorSyncAsync(ColorSync color, CancellationToken ct = default)
    {
        await _bus.Publish(new ColorSyncMessage(color), ct);
        _logger.LogDebug("[BUS] Published ColorSyncMessage — Code={Code}", color.Code);
    }

    public async Task PublishSizeSyncAsync(SizeSync size, CancellationToken ct = default)
    {
        await _bus.Publish(new SizeSyncMessage(size), ct);
        _logger.LogDebug("[BUS] Published SizeSyncMessage — Code={Code}", size.Code);
    }
}
