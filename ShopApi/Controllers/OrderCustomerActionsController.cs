using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/orders")]
public sealed class OrderCustomerActionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrderCustomerActionsController(AppDbContext db) => _db = db;

    [Authorize]
    [HttpGet("{orderId}/invoice")]
    public async Task<IActionResult> Invoice(string orderId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        return Ok(new { success = true, data = new { invoiceNumber = order.Id, issuedAt = order.CreatedAt,
            seller = "Tadabor Life", customer = new { order.CustomerName, order.CustomerPhone, order.Province, order.City, order.PostalAddress, order.PostalCode },
            items = order.Items.Select(i => new { i.ProductTitle, i.Quantity, i.UnitPrice, i.TotalPrice }),
            order.ItemsPrice, order.DiscountAmount, order.CouponCode, order.ShippingFee, order.TotalPayable,
            order.PaymentMethod, order.Status, order.PaymentRefId }, message = "Invoice retrieved." });
    }

    [Authorize]
    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> Cancel(string orderId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        if (order.Status != "pending_payment") return Conflict(Failure("CANCELLATION_UNAVAILABLE", "Only unpaid orders can be cancelled automatically. Request a refund for paid orders."));
        await RestoreStock(order, ct); order.Status = "cancelled"; order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(new { success = true, data = new { order.Id, order.Status }, message = "Order cancelled." });
    }

    [Authorize]
    [HttpPost("{orderId}/refund-requests")]
    public async Task<IActionResult> RequestRefund(string orderId, [FromBody] RefundInput request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(Failure("REASON_REQUIRED", "Refund reason is required."));
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        if (order.Status is not ("paid" or "processing" or "shipped" or "delivered")) return Conflict(Failure("REFUND_UNAVAILABLE", "This order is not eligible for a refund request."));
        if (await _db.RefundRequests.AnyAsync(r => r.OrderId == orderId && r.UserId == userId && r.Status == "requested", ct)) return Conflict(Failure("REFUND_EXISTS", "A refund request is already open."));
        var refund = new RefundRequest { OrderId = orderId, UserId = userId, Reason = request.Reason.Trim() };
        _db.RefundRequests.Add(refund); await _db.SaveChangesAsync(ct);
        return StatusCode(201, new { success = true, data = new { refund.Id, refund.OrderId, refund.Status, refund.CreatedAt }, message = "Refund request submitted for review." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{orderId}/shipment")]
    public async Task<IActionResult> UpdateShipment(string orderId, [FromBody] ShipmentInput request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TrackingNumber)) return BadRequest(Failure("TRACKING_REQUIRED", "Carrier tracking number is required."));
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        if (order.Status is not ("paid" or "processing" or "shipped")) return Conflict(Failure("INVALID_ORDER_STATUS", "Only paid or processing orders can be shipped."));
        order.TrackingNumber = request.TrackingNumber.Trim(); order.Status = "shipped"; order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new { order.Id, order.Status, order.TrackingNumber }, message = "Shipment tracking saved. Live carrier tracking requires a configured carrier API." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateStatus(string orderId, [FromBody] OrderStatusInput request, CancellationToken ct)
    {
        var allowed = new[] { "processing", "shipped", "delivered" };
        if (!allowed.Contains(request.Status)) return BadRequest(Failure("INVALID_STATUS", "Allowed statuses are processing, shipped, and delivered."));
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return NotFound(Failure("ORDER_NOT_FOUND", "Order not found."));
        if (order.Status is not ("paid" or "processing" or "shipped")) return Conflict(Failure("INVALID_TRANSITION", "Order cannot transition from its current status."));
        order.Status = request.Status; order.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new { order.Id, order.Status }, message = "Order status updated." });
    }

    private async Task RestoreStock(Order order, CancellationToken ct)
    {
        var ids = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var line in order.Items) if (products.TryGetValue(line.ProductId, out var product)) product.Stock += line.Quantity;
    }
    private static object Failure(string code, string message) => new { success = false, error = new { code, message } };
}
public sealed record RefundInput(string Reason);
public sealed record ShipmentInput(string TrackingNumber);
public sealed record OrderStatusInput(string Status);
