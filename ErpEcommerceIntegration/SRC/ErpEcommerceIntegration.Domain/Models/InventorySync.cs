namespace ErpEcommerceIntegration.Domain.Models;

public sealed class InventorySync
{
    public string Sku { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public string WarehouseCode { get; init; } = string.Empty;
}
