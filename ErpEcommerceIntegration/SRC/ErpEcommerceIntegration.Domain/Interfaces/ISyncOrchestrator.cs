namespace ErpEcommerceIntegration.Domain.Interfaces;

public interface ISyncOrchestrator
{
    Task SynchronizeProductsAsync(CancellationToken ct = default);
    Task SynchronizeInventoryAsync(CancellationToken ct = default);
    Task SynchronizeColorsAsync(CancellationToken ct = default);
    Task SynchronizeSizesAsync(CancellationToken ct = default);
}
