namespace ErpEcommerceIntegration.Domain.Models;

public sealed class ColorSync
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
