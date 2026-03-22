using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.MessageBus.Consumers;

public sealed class SyncInventoryConsumer : IConsumer<InventorySyncMessage>
{
    private readonly IECommerceEndPoint _eCommerce;
    private readonly ILogger<SyncInventoryConsumer> _logger;

    public SyncInventoryConsumer(IECommerceEndPoint eCommerce, ILogger<SyncInventoryConsumer> logger)
    {
        _eCommerce = eCommerce;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InventorySyncMessage> context)
    {
        var inventory = context.Message.Inventory;
        _logger.LogInformation("[CONSUMER] Processing InventorySyncMessage — SKU={Sku}", inventory.Sku);

        var success = await _eCommerce.UpdateInventoryAsync(inventory, context.CancellationToken);

        if (!success)
            throw new InvalidOperationException($"E-commerce rejected inventory update SKU={inventory.Sku}");

        _logger.LogInformation("[CONSUMER] InventorySyncMessage completed — SKU={Sku}", inventory.Sku);
    }
}
