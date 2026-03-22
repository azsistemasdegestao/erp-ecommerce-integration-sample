using ErpEcommerceIntegration.Domain.Models;

namespace ErpEcommerceIntegration.Domain.Interfaces;

public interface IERPDatabaseService
{
    /// <summary>In production: Dapper query against legacy ERP SQL view VW_PRODUCTS.</summary>
    Task<IEnumerable<ProductSync>> GetProductsAsync(DateTime? updatedSince = null, CancellationToken ct = default);

    /// <summary>In production: Dapper query against ERP view VW_INVENTORY.</summary>
    Task<IEnumerable<InventorySync>> GetInventoryAsync(string[]? skus = null, CancellationToken ct = default);

    /// <summary>In production: Dapper query against ERP view VW_COLORS.</summary>
    Task<IEnumerable<ColorSync>> GetColorsAsync(CancellationToken ct = default);

    /// <summary>In production: Dapper query against ERP view VW_SIZES.</summary>
    Task<IEnumerable<SizeSync>> GetSizesAsync(CancellationToken ct = default);
}
