using ErpEcommerceIntegration.Infra.ERPDatabase;
using Microsoft.Extensions.Logging.Abstractions;

namespace ErpEcommerceIntegration.IntegrationTests.ERP;

/// <summary>
/// Integration tests for ERPDatabaseService.
/// Uses the static mock data that simulates VW_PRODUCTS, VW_INVENTORY, VW_COLORS, VW_SIZES.
/// In production these same tests would run against a real DB via a test container or seeded schema.
/// </summary>
public sealed class ERPDatabaseServiceTests
{
    private readonly ERPDatabaseService _sut = new(NullLogger<ERPDatabaseService>.Instance);

    // ── Products ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsAsync_WithoutFilter_ReturnsAllFiveProducts()
    {
        var result = (await _sut.GetProductsAsync()).ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetProductsAsync_WithUpdatedSinceRecent_ReturnsOnlyRecentProducts()
    {
        // Only SKU-004 was updated within the last hour (AddMinutes(-30))
        var cutoff = DateTime.UtcNow.AddHours(-1).AddMinutes(1);

        var result = (await _sut.GetProductsAsync(updatedSince: cutoff)).ToList();

        Assert.Single(result);
        Assert.Equal("SKU-004", result[0].Sku);
    }

    [Fact]
    public async Task GetProductsAsync_WithOldCutoff_ReturnsNoProducts()
    {
        var cutoff = DateTime.UtcNow.AddDays(1); // future date — nothing qualifies

        var result = (await _sut.GetProductsAsync(updatedSince: cutoff)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProductsAsync_MockDataHasFourActiveAndOneInactive()
    {
        var products = (await _sut.GetProductsAsync()).ToList();

        Assert.Equal(4, products.Count(p => p.IsActive));
        Assert.Equal(1, products.Count(p => !p.IsActive));
    }

    [Fact]
    public async Task GetProductsAsync_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.GetProductsAsync(ct: cts.Token));
    }

    // ── Inventory ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInventoryAsync_WithoutFilter_ReturnsAllFiveItems()
    {
        var result = (await _sut.GetInventoryAsync()).ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetInventoryAsync_WithSkuFilter_ReturnsOnlyMatchingSkus()
    {
        var skus = new[] { "SKU-001", "SKU-003" };

        var result = (await _sut.GetInventoryAsync(skus: skus)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Contains(item.Sku, skus));
    }

    [Fact]
    public async Task GetInventoryAsync_WithUnknownSku_ReturnsEmpty()
    {
        var result = (await _sut.GetInventoryAsync(skus: new[] { "SKU-999" })).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInventoryAsync_WithEmptySkuArray_ReturnsAllItems()
    {
        // Empty array means no filter — same as passing null
        var result = (await _sut.GetInventoryAsync(skus: Array.Empty<string>())).ToList();

        Assert.Equal(5, result.Count);
    }

    // ── Colors ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetColorsAsync_ReturnsFourColors()
    {
        var result = (await _sut.GetColorsAsync()).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetColorsAsync_ContainsExpectedCodes()
    {
        var result = (await _sut.GetColorsAsync()).ToList();
        var codes = result.Select(c => c.Code).ToHashSet();

        Assert.Contains("BLU", codes);
        Assert.Contains("RED", codes);
        Assert.Contains("BLK", codes);
        Assert.Contains("WHT", codes);
    }

    // ── Sizes ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSizesAsync_ReturnsFourSizes()
    {
        var result = (await _sut.GetSizesAsync()).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetSizesAsync_ContainsExpectedCodes()
    {
        var result = (await _sut.GetSizesAsync()).ToList();
        var codes = result.Select(s => s.Code).ToHashSet();

        Assert.Contains("S", codes);
        Assert.Contains("M", codes);
        Assert.Contains("L", codes);
        Assert.Contains("XL", codes);
    }
}
