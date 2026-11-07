using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShopApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/shop/shipping")]
public sealed class ShippingController : ControllerBase
{
    private const decimal Threshold = 400000m;
    private const decimal Fee = 35000m;

    [HttpPost("calculate")]
    public IActionResult Calculate([FromBody] ShippingRequest request)
    {
        if (request.CartAmount < 0) return BadRequest(new { success = false, error = new { code = "INVALID_AMOUNT", message = "Cart amount cannot be negative." } });
        var free = request.CartAmount >= Threshold;
        return Ok(new { success = true, data = new { shippingFee = free ? 0m : Fee,
            isFreeShipping = free, freeShippingThreshold = Threshold,
            remainingForFreeShipping = Math.Max(0m, Threshold - request.CartAmount) }, message = "Shipping fee calculated successfully." });
    }
}

public sealed record ShippingRequest(string Province, string City, decimal CartAmount);
