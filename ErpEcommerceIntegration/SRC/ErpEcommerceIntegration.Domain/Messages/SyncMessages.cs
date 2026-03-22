using ErpEcommerceIntegration.Domain.Models;

namespace ErpEcommerceIntegration.Domain.Messages;

// MassTransit message contracts — use records for immutability
public sealed record ProductSyncMessage(ProductSync Product);
public sealed record InventorySyncMessage(InventorySync Inventory);
public sealed record ColorSyncMessage(ColorSync Color);
public sealed record SizeSyncMessage(SizeSync Size);
