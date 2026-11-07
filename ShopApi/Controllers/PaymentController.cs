using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    public PaymentController(AppDbContext db, IWebHostEnvironment environment) { _db = db; _environment = environment; }

    [Authorize]
    [HttpPost("request")]
    public async Task<IActionResult> RequestPayment([FromBody] PaymentRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        if (order.Status != "pending_payment") return Conflict(Failure("INVALID_ORDER_STATUS", "Only pending orders can be paid."));
        // This project currently has no bank credentials/provider adapter. Keep this explicit mock out of production.
        if (!_environment.IsDevelopment()) return StatusCode(503, Failure("PAYMENT_NOT_CONFIGURED", "Configure a verified payment provider before enabling payments."));
        var authority = Guid.NewGuid().ToString("N");
        order.PaymentRefId = authority;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new { orderId = order.Id, authority,
            paymentUrl = $"/api/v1/shop/payment/mock-gateway?Authority={authority}" }, message = "Development mock payment created." });
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string? Authority, [FromQuery] string? Status, CancellationToken ct)
    {
        if (!_environment.IsDevelopment()) return StatusCode(503, Failure("PAYMENT_NOT_CONFIGURED", "Payment verification requires a configured bank provider."));
        if (string.IsNullOrWhiteSpace(Authority) || string.IsNullOrWhiteSpace(Status))
            return BadRequest(Failure("INVALID_CALLBACK", "Authority and Status are required."));
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.PaymentRefId == Authority, ct);
        if (order is null) return NotFound(Failure("PAYMENT_NOT_FOUND", "No payment matches this authority."));
        if (order.Status != "pending_payment") return Conflict(Failure("PAYMENT_ALREADY_PROCESSED", "This payment has already been processed."));
        if (Status.Equals("OK", StringComparison.OrdinalIgnoreCase)) order.Status = "paid";
        else
        {
            order.Status = "cancelled";
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
            foreach (var item in order.Items) if (products.TryGetValue(item.ProductId, out var product)) product.Stock += item.Quantity;
        }
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new { orderId = order.Id, status = order.Status,
            paymentRefId = order.Status == "paid" ? $"MOCK-{Authority}" : null }, message = "Development mock payment processed." });
    }

    private static object Failure(string code, string message) => new { success = false, error = new { code, message, details = (object?)null } };
}

public sealed record PaymentRequest(string OrderId);
