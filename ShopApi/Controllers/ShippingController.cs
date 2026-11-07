using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<object>>> RequestPayment([FromBody] PaymentRequestDto request)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Order not found."));
        }

        var authority = $"AUTH-{Guid.NewGuid().ToString()[..12].ToUpper()}";
        return Ok(new ApiResponse<object>(true, new
        {
            orderId = order.Id,
            authority,
            paymentUrl = $"https://gateway.mock/StartPay?Authority={authority}&OrderId={order.Id}"
        }, "Payment request created successfully."));
    }

    [AllowAnonymous]
    [HttpGet("verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new ApiResponse<object>(false, null, "Authority and status are required."));
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentRefId == authority || o.Status == "pending_payment");
        if (order is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Pending order not found."));
        }

        order.Status = status == "OK" ? "paid" : "cancelled";
        order.PaymentRefId = authority;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, new
        {
            orderId = order.Id,
            status = order.Status,
            paymentRefId = authority
        }, "Payment verification completed."));
    }
}
