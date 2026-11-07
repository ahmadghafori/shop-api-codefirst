using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopApi.Models;

namespace ShopApi.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<ShopCoupon> ShopCoupons => Set<ShopCoupon>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
        modelBuilder.Entity<Product>(e => {
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.OriginalPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.Rating).HasColumnType("decimal(3,1)");
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.Property(p => p.Category).HasMaxLength(50);
            e.HasIndex(p => p.Category);
        });
        modelBuilder.Entity<CartItem>(e => {
            e.HasOne(x => x.Product).WithMany(x => x.CartItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(x => x.CartItems).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.GuestSessionId, x.ProductId }).IsUnique();
        });
        modelBuilder.Entity<Order>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemsPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPayable).HasColumnType("decimal(18,2)");
            e.Property(x => x.CouponCode).HasMaxLength(50);
            e.HasOne(x => x.User).WithMany(x => x.Orders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrderItem>(e => {
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Payment>(e => {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Authority).IsUnique();
            e.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CustomerAddress>(e => {
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.IsDefault });
        });
        modelBuilder.Entity<ShopCoupon>(e => {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.DiscountValue).HasColumnType("decimal(18,2)");
            e.Property(x => x.MinimumOrderAmount).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<RefundRequest>(e => {
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrderId, x.UserId });
        });
    }
}
