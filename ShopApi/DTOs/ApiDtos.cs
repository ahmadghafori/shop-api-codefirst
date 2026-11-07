using Microsoft.AspNetCore.Identity;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Customer"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@shopapi.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Shop",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { Name = "Electronics", Description = "Electronic gadgets and accessories" },
                new Category { Name = "Fashion", Description = "Clothing and personal accessories" },
                new Category { Name = "Home", Description = "Home and living essentials" },
                new Category { Name = "Sports", Description = "Sports and outdoor products" }
            );
        }

        if (!context.Products.Any())
        {
            var categories = context.Categories.ToList();
            var electronics = categories.First(c => c.Name == "Electronics");
            var fashion = categories.First(c => c.Name == "Fashion");
            var home = categories.First(c => c.Name == "Home");
            var sports = categories.First(c => c.Name == "Sports");

            context.Products.AddRange(
                new Product
                {
                    Name = "Wireless Headphones",
                    Slug = "wireless-headphones",
                    Description = "High quality noise cancelling wireless headphones.",
                    Price = 199.99m,
                    StockQuantity = 40,
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b",
                    CategoryId = electronics.Id
                },
                new Product
                {
                    Name = "Smart Watch",
                    Slug = "smart-watch",
                    Description = "Track your fitness and get notifications on the go.",
                    Price = 249.99m,
                    StockQuantity = 30,
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30",
                    CategoryId = electronics.Id
                },
                new Product
                {
                    Name = "Classic Cotton T-Shirt",
                    Slug = "classic-cotton-t-shirt",
                    Description = "Lightweight and comfortable everyday shirt.",
                    Price = 29.99m,
                    StockQuantity = 80,
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518",
                    CategoryId = fashion.Id
                },
                new Product
                {
                    Name = "Leather Wallet",
                    Slug = "leather-wallet",
                    Description = "Premium quality wallet with enough space for cards and cash.",
                    Price = 59.99m,
                    StockQuantity = 55,
                    IsFeatured = false,
                    ImageUrl = "https://images.unsplash.com/photo-1627123424574-724758594e93",
                    CategoryId = fashion.Id
                },
                new Product
                {
                    Name = "Ceramic Coffee Mug",
                    Slug = "ceramic-coffee-mug",
                    Description = "Modern mug for your coffee and tea moments.",
                    Price = 18.50m,
                    StockQuantity = 100,
                    IsFeatured = false,
                    ImageUrl = "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d",
                    CategoryId = home.Id
                },
                new Product
                {
                    Name = "Yoga Mat",
                    Slug = "yoga-mat",
                    Description = "Soft and durable mat for workouts and stretching.",
                    Price = 42.00m,
                    StockQuantity = 65,
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1517836357463-d25dfeac3438",
                    CategoryId = sports.Id
                }
            );
        }

        await context.SaveChangesAsync();
    }
}
