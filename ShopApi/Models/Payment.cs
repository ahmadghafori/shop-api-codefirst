namespace ShopApi.Models;

public class Payment
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public Order? Order { get; set; }
    public decimal Amount { get; set; }
    public string Gateway { get; set; } = "zarinpal";
    public string Authority { get; set; } = string.Empty;
    public string? RefId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}
