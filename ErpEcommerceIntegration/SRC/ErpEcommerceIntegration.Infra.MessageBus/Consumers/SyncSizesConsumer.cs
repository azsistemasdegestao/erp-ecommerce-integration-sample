using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.MessageBus.Consumers;

public sealed class SyncSizesConsumer : IConsumer<SizeSyncMessage>
{
    private readonly IECommerceEndPoint _eCommerce;
    private readonly ILogger<SyncSizesConsumer> _logger;

    public SyncSizesConsumer(IECommerceEndPoint eCommerce, ILogger<SyncSizesConsumer> logger)
    {
        _eCommerce = eCommerce;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SizeSyncMessage> context)
    {
        var size = context.Message.Size;
        _logger.LogInformation("[CONSUMER] Processing SizeSyncMessage — Code={Code}", size.Code);

        var success = await _eCommerce.UpsertSizeAsync(size, context.CancellationToken);

        if (!success)
            throw new InvalidOperationException($"E-commerce rejected size Code={size.Code}");

        _logger.LogInformation("[CONSUMER] SizeSyncMessage completed — Code={Code}", size.Code);
    }
}
