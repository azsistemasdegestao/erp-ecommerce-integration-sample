namespace ErpEcommerceIntegration.Domain.Models;

/// <summary>
/// Represents a product record fetched from the ERP system,
/// ready to be synced to the e-commerce platform.
/// </summary>
public sealed class ProductSync
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ColorCode { get; init; } = string.Empty;
    public string SizeCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime UpdatedAtERP { get; init; }
}
