using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Models;
using ErpEcommerceIntegration.Infra.ERPDatabase.MockViews;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.ERPDatabase;

public sealed class ERPDatabaseService : IERPDatabaseService
{
    private readonly ILogger<ERPDatabaseService> _logger;

    public ERPDatabaseService(ILogger<ERPDatabaseService> logger) => _logger = logger;

    public async Task<IEnumerable<ProductSync>> GetProductsAsync(DateTime? updatedSince = null, CancellationToken ct = default)
    {
        // Simulate DB latency (replace with: await connection.QueryAsync<ProductSync>("SELECT ... FROM VW_PRODUCTS ..."))
        await Task.Delay(60, ct);

        var results = ERPMockData.VW_PRODUCTS.AsEnumerable();
        if (updatedSince.HasValue)
            results = results.Where(p => p.UpdatedAtERP >= updatedSince.Value);

        _logger.LogDebug("[ERP] VW_PRODUCTS returned {Count} rows (filter updatedSince={Since})", results.Count(), updatedSince);
        return results;
    }

    public async Task<IEnumerable<InventorySync>> GetInventoryAsync(string[]? skus = null, CancellationToken ct = default)
    {
        await Task.Delay(40, ct);
        var results = ERPMockData.VW_INVENTORY.AsEnumerable();
        if (skus is { Length: > 0 })
            results = results.Where(i => skus.Contains(i.Sku));
        return results;
    }

    public async Task<IEnumerable<ColorSync>> GetColorsAsync(CancellationToken ct = default)
    {
        await Task.Delay(20, ct);
        return ERPMockData.VW_COLORS;
    }

    public async Task<IEnumerable<SizeSync>> GetSizesAsync(CancellationToken ct = default)
    {
        await Task.Delay(20, ct);
        return ERPMockData.VW_SIZES;
    }
}
