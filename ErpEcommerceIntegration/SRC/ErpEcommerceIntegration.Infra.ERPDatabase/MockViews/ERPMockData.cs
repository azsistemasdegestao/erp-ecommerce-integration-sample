using ErpEcommerceIntegration.Domain.Models;

namespace ErpEcommerceIntegration.Infra.ERPDatabase.MockViews;

/// <summary>
/// Static mock data simulating ERP SQL views.
///
/// PRODUCTION REPLACEMENT: Each property maps to a real SQL view:
///   VW_PRODUCTS  → SELECT * FROM VW_PRODUCTS WHERE UPDATED_AT >= :since
///   VW_INVENTORY → SELECT * FROM VW_INVENTORY WHERE SKU IN (:skus)
///   VW_COLORS    → SELECT * FROM VW_COLORS
///   VW_SIZES     → SELECT * FROM VW_SIZES
///
/// Real implementation uses Dapper with a legacy Firebird or SQL Server connection.
/// Replace this class with a Dapper-based repository and inject IDbConnection.
/// </summary>
internal static class ERPMockData
{
    internal static readonly IReadOnlyList<ProductSync> VW_PRODUCTS = new List<ProductSync>
    {
        new() { Sku = "SKU-001", Name = "Classic T-Shirt Blue M",   Description = "100% cotton",  Price = 59.90m,  ColorCode = "BLU", SizeCode = "M",  IsActive = true,  UpdatedAtERP = DateTime.UtcNow.AddHours(-1) },
        new() { Sku = "SKU-002", Name = "Classic T-Shirt Blue L",   Description = "100% cotton",  Price = 59.90m,  ColorCode = "BLU", SizeCode = "L",  IsActive = true,  UpdatedAtERP = DateTime.UtcNow.AddHours(-1) },
        new() { Sku = "SKU-003", Name = "Classic T-Shirt Red M",    Description = "100% cotton",  Price = 64.90m,  ColorCode = "RED", SizeCode = "M",  IsActive = true,  UpdatedAtERP = DateTime.UtcNow.AddHours(-3) },
        new() { Sku = "SKU-004", Name = "Premium Polo Black XL",    Description = "Slim fit",     Price = 89.90m,  ColorCode = "BLK", SizeCode = "XL", IsActive = true,  UpdatedAtERP = DateTime.UtcNow.AddMinutes(-30) },
        new() { Sku = "SKU-005", Name = "Discontinued Sample Item", Description = "Discontinued", Price = 0m,      ColorCode = "WHT", SizeCode = "S",  IsActive = false, UpdatedAtERP = DateTime.UtcNow.AddDays(-10) },
    };

    internal static readonly IReadOnlyList<InventorySync> VW_INVENTORY = new List<InventorySync>
    {
        new() { Sku = "SKU-001", QuantityAvailable = 120, WarehouseCode = "WH-MAIN"      },
        new() { Sku = "SKU-002", QuantityAvailable = 85,  WarehouseCode = "WH-MAIN"      },
        new() { Sku = "SKU-003", QuantityAvailable = 43,  WarehouseCode = "WH-MAIN"      },
        new() { Sku = "SKU-004", QuantityAvailable = 12,  WarehouseCode = "WH-SECONDARY" },
        new() { Sku = "SKU-005", QuantityAvailable = 0,   WarehouseCode = "WH-MAIN"      },
    };

    internal static readonly IReadOnlyList<ColorSync> VW_COLORS = new List<ColorSync>
    {
        new() { Id = 1, Code = "BLU", Label = "Blue"  },
        new() { Id = 2, Code = "RED", Label = "Red"   },
        new() { Id = 3, Code = "BLK", Label = "Black" },
        new() { Id = 4, Code = "WHT", Label = "White" },
    };

    internal static readonly IReadOnlyList<SizeSync> VW_SIZES = new List<SizeSync>
    {
        new() { Id = 1, Code = "S",  Label = "Small"       },
        new() { Id = 2, Code = "M",  Label = "Medium"      },
        new() { Id = 3, Code = "L",  Label = "Large"       },
        new() { Id = 4, Code = "XL", Label = "Extra Large" },
    };
}
