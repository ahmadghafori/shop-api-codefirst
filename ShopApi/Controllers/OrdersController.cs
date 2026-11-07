using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var orders = await _db.Orders.AsNoTracking().Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).Select(o => new { o.Id, date = o.CreatedAt,
                o.TrackingNumber, o.Status, o.TotalPayable, o.ItemsPrice, o.DiscountAmount, o.ShippingFee,
                customer = new { fullName = o.CustomerName, phone = o.CustomerPhone, o.Province, o.City,
                    postalAddress = o.PostalAddress, postalCode = o.PostalCode, o.Notes },
                items = o.Items.Select(i => new { i.ProductId, title = i.ProductTitle, i.Quantity,
                    unitPrice = i.UnitPrice, i.TotalPrice }) })
            .ToListAsync(ct);
        return Ok(new { success = true, data = orders, message = "Orders retrieved successfully." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CheckoutInput request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        if (request.Customer is null || string.IsNullOrWhiteSpace(request.Customer.FullName) ||
            string.IsNullOrWhiteSpace(request.Customer.Phone) || string.IsNullOrWhiteSpace(request.Customer.Province) ||
            string.IsNullOrWhiteSpace(request.Customer.City) || string.IsNullOrWhiteSpace(request.Customer.PostalAddress))
            return BadRequest(Failure("INVALID_INPUT", "Recipient name, phone, province, city, and postal address are required."));
        if (request.PaymentMethod is not ("zarinpal" or "saman" or "card_to_card" or "cod"))
            return BadRequest(Failure("INVALID_PAYMENT_METHOD", "Unsupported payment method."));

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var cart = await _db.CartItems.Include(x => x.Product).Where(x => x.UserId == userId).ToListAsync(ct);
        if (cart.Count == 0) return BadRequest(Failure("EMPTY_CART", "Your cart is empty."));
        if (cart.Any(x => x.Product is null || !x.Product.IsActive || x.Quantity > x.Product.Stock))
            return Conflict(Failure("OUT_OF_STOCK", "One or more products are unavailable or have insufficient stock."));

        var order = new Order { Id = $"TB-{Guid.NewGuid():N}"[..13].ToUpperInvariant(), UserId = userId,
            CustomerName = request.Customer.FullName.Trim(), CustomerPhone = request.Customer.Phone.Trim(),
            Province = request.Customer.Province.Trim(), City = request.Customer.City.Trim(),
            PostalAddress = request.Customer.PostalAddress.Trim(), PostalCode = request.Customer.PostalCode,
            Notes = request.Customer.Notes, PaymentMethod = request.PaymentMethod, Status = "pending_payment",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        foreach (var line in cart)
        {
            var product = line.Product!;
            var amount = product.Price * line.Quantity;
            order.ItemsPrice += product.OriginalPrice * line.Quantity;
            order.DiscountAmount += Math.Max(0m, product.OriginalPrice - product.Price) * line.Quantity;
            order.Items.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, ProductTitle = product.Title,
                Quantity = line.Quantity, UnitPrice = product.Price, TotalPrice = amount });
            // Reserve stock while payment is pending; failed mock payments restore it below.
            product.Stock -= line.Quantity;
        }
        order.ShippingFee = order.Items.Sum(i => i.TotalPrice) >= 400000m ? 0m : 35000m;
        order.TotalPayable = order.Items.Sum(i => i.TotalPrice) + order.ShippingFee;
        _db.Orders.Add(order); _db.CartItems.RemoveRange(cart);
        await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return StatusCode(201, new { success = true, data = new { orderId = order.Id, trackingNumber = (string?)null,
            totalPayable = order.TotalPayable, status = order.Status,
            paymentUrl = $"/api/v1/shop/payment/request" }, message = "Order created successfully." });
    }

    [AllowAnonymous]
    [HttpGet("track/{trackingNumber}")]
    public async Task<IActionResult> TrackOrder(string trackingNumber, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber, ct);
        if (order is null) return NotFound(Failure("TRACKING_NOT_FOUND", "Shipment tracking number not found."));
        return Ok(new { success = true, data = new { trackingNumber, order.Status, order.UpdatedAt,
            message = "This is the shop order status; carrier tracking requires a postal service integration." } });
    }

    private static object Failure(string code, string message) => new { success = false, error = new { code, message, details = (object?)null } };
}

public sealed record CustomerInput(string FullName, string Phone, string Province, string City, string PostalAddress, string? PostalCode, string? Notes);
public sealed record CheckoutInput(CustomerInput Customer, string PaymentMethod = "zarinpal");
