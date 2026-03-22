using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.MessageBus.Consumers;

public sealed class SyncProductsConsumer : IConsumer<ProductSyncMessage>
{
    private readonly IECommerceEndPoint _eCommerce;
    private readonly ILogger<SyncProductsConsumer> _logger;

    public SyncProductsConsumer(IECommerceEndPoint eCommerce, ILogger<SyncProductsConsumer> logger)
    {
        _eCommerce = eCommerce;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductSyncMessage> context)
    {
        var product = context.Message.Product;
        _logger.LogInformation("[CONSUMER] Processing ProductSyncMessage — SKU={Sku}", product.Sku);

        var success = await _eCommerce.UpsertProductAsync(product, context.CancellationToken);

        if (!success)
            throw new InvalidOperationException($"E-commerce rejected product SKU={product.Sku}");

        _logger.LogInformation("[CONSUMER] ProductSyncMessage completed — SKU={Sku}", product.Sku);
    }
}
