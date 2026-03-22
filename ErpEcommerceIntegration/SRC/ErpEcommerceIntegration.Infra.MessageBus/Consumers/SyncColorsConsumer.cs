using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.MessageBus.Consumers;

public sealed class SyncColorsConsumer : IConsumer<ColorSyncMessage>
{
    private readonly IECommerceEndPoint _eCommerce;
    private readonly ILogger<SyncColorsConsumer> _logger;

    public SyncColorsConsumer(IECommerceEndPoint eCommerce, ILogger<SyncColorsConsumer> logger)
    {
        _eCommerce = eCommerce;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ColorSyncMessage> context)
    {
        var color = context.Message.Color;
        _logger.LogInformation("[CONSUMER] Processing ColorSyncMessage — Code={Code}", color.Code);

        var success = await _eCommerce.UpsertColorAsync(color, context.CancellationToken);

        if (!success)
            throw new InvalidOperationException($"E-commerce rejected color Code={color.Code}");

        _logger.LogInformation("[CONSUMER] ColorSyncMessage completed — Code={Code}", color.Code);
    }
}
