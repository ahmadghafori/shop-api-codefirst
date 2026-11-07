namespace ShopApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "books";
    public string CategoryTitle { get; set; } = string.Empty;
    public string ShortDesc { get; set; } = string.Empty;
    public string FullDesc { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public string? Badge { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AuthorOrPublisher { get; set; }
    public int? Pages { get; set; }
    public decimal Rating { get; set; } = 5.0m;
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
