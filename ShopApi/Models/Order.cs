namespace ShopApi.Models;

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalAddress { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }
    public decimal ItemsPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalPayable { get; set; }
    public string? CouponCode { get; set; }
    public string PaymentMethod { get; set; } = "zarinpal";
    public string Status { get; set; } = "pending_payment";
    public string? TrackingNumber { get; set; }
    public string? PaymentRefId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
