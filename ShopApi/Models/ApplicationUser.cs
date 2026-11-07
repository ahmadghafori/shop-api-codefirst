using Microsoft.AspNetCore.Identity;

namespace ShopApi.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
