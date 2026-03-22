using ErpEcommerceIntegration.Domain.Models;

namespace ErpEcommerceIntegration.Domain.Interfaces;

public interface IECommerceEndPoint
{
    /// <summary>In production: HTTP PUT /api/v1/products/{sku} with Bearer token.</summary>
    Task<bool> UpsertProductAsync(ProductSync product, CancellationToken ct = default);

    /// <summary>In production: HTTP PUT /api/v1/inventory/{sku}.</summary>
    Task<bool> UpdateInventoryAsync(InventorySync inventory, CancellationToken ct = default);

    /// <summary>In production: HTTP POST /api/v1/attributes/colors.</summary>
    Task<bool> UpsertColorAsync(ColorSync color, CancellationToken ct = default);

    /// <summary>In production: HTTP POST /api/v1/attributes/sizes.</summary>
    Task<bool> UpsertSizeAsync(SizeSync size, CancellationToken ct = default);
}
