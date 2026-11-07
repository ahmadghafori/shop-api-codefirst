namespace ShopApi.Models;

public sealed class ShopCoupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsPercentage { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
