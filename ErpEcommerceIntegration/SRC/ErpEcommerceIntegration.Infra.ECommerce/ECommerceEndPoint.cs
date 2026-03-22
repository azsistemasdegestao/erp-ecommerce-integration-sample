using ErpEcommerceIntegration.Domain.Interfaces;
using ErpEcommerceIntegration.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ErpEcommerceIntegration.Infra.ECommerce;

/// <summary>
/// Mock HTTP client for the e-commerce platform REST API.
///
/// PRODUCTION REPLACEMENT:
///   - Use IHttpClientFactory with a named client "ECommerce"
///   - Add Bearer token via DelegatingHandler
///   - Base URL and token come from IConfiguration["ECommerce:BaseUrl"] / ["ECommerce:ApiToken"]
///   - Wrap calls with Polly retry (already registered in IoC via AddHttpClient + AddPolicyHandler)
///   - Map domain models to API-specific request DTOs before sending
/// </summary>
public sealed class ECommerceEndPoint : IECommerceEndPoint
{
    private readonly ILogger<ECommerceEndPoint> _logger;

    // Simulate a ~5% transient failure rate to demonstrate retry behavior
    private static readonly Random _jitter = new();

    public ECommerceEndPoint(ILogger<ECommerceEndPoint> logger) => _logger = logger;

    public async Task<bool> UpsertProductAsync(ProductSync product, CancellationToken ct = default)
    {
        await SimulateHttpCallAsync(ct);
        // Production: await _httpClient.PutAsJsonAsync($"/api/v1/products/{product.Sku}", requestDto, ct)
        _logger.LogInformation("[ECOMMERCE] Product upserted — SKU={Sku} Name={Name} Price={Price:C}", product.Sku, product.Name, product.Price);
        return true;
    }

    public async Task<bool> UpdateInventoryAsync(InventorySync inventory, CancellationToken ct = default)
    {
        await SimulateHttpCallAsync(ct);
        // Production: await _httpClient.PutAsJsonAsync($"/api/v1/inventory/{inventory.Sku}", requestDto, ct)
        _logger.LogInformation("[ECOMMERCE] Inventory updated — SKU={Sku} Qty={Qty} Warehouse={WH}", inventory.Sku, inventory.QuantityAvailable, inventory.WarehouseCode);
        return true;
    }

    public async Task<bool> UpsertColorAsync(ColorSync color, CancellationToken ct = default)
    {
        await SimulateHttpCallAsync(ct);
        _logger.LogInformation("[ECOMMERCE] Color upserted — Code={Code} Label={Label}", color.Code, color.Label);
        return true;
    }

    public async Task<bool> UpsertSizeAsync(SizeSync size, CancellationToken ct = default)
    {
        await SimulateHttpCallAsync(ct);
        _logger.LogInformation("[ECOMMERCE] Size upserted — Code={Code} Label={Label}", size.Code, size.Label);
        return true;
    }

    /// <summary>Simulates HTTP round-trip latency and occasional transient failures.</summary>
    private static async Task SimulateHttpCallAsync(CancellationToken ct)
    {
        await Task.Delay(_jitter.Next(50, 150), ct);
        if (_jitter.Next(100) < 5)
            throw new HttpRequestException("Simulated transient e-commerce API error — Polly will retry.");
    }
}
