using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guestSessionId = GetGuestSessionId();

        var query = _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => (userId != null && ci.UserId == userId) || (!string.IsNullOrWhiteSpace(guestSessionId) && ci.GuestSessionId == guestSessionId));

        var cartItems = await query
            .Select(ci => new CartItemDto(
                ci.ProductId,
                ci.Quantity,
                new ProductDto(
                    ci.Product!.Id,
                    ci.Product.Title,
                    ci.Product.Category,
                    ci.Product.CategoryTitle,
                    ci.Product.ShortDesc,
                    ci.Product.FullDesc,
                    ci.Product.Price,
                    ci.Product.OriginalPrice,
                    ci.Product.Badge,
                    ci.Product.ImageUrl,
                    ci.Product.AuthorOrPublisher,
                    ci.Product.Pages,
                    ci.Product.Rating,
                    ci.Product.Stock,
                    ci.Product.IsActive),
                ci.Product.Price,
                ci.Product.Price * ci.Quantity))
            .ToListAsync();

        var summary = new CartSummaryDto(
            cartItems.Sum(x => x.Quantity),
            cartItems.Sum(x => x.TotalPrice),
            0m,
            0m,
            cartItems.Sum(x => x.TotalPrice));

        return Ok(new ApiResponse<object>(true, new { items = cartItems, summary }, "Cart loaded successfully."));
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<object>>> AddToCart([FromBody] AddCartItemRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guestSessionId = GetGuestSessionId();

        if (request.Quantity <= 0)
        {
            return BadRequest(new ApiResponse<object>(false, null, "Quantity must be greater than zero."));
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Product not found."));
        }

        if (request.Quantity > product.Stock)
        {
            return BadRequest(new ApiResponse<object>(false, null, "Requested quantity exceeds available stock."));
        }

        var existingItem = await _context.CartItems.FirstOrDefaultAsync(ci =>
            ci.ProductId == request.ProductId &&
            ((userId != null && ci.UserId == userId) || (!string.IsNullOrWhiteSpace(guestSessionId) && ci.GuestSessionId == guestSessionId)));

        if (existingItem is not null)
        {
            var newQty = existingItem.Quantity + request.Quantity;
            if (newQty > product.Stock)
            {
                return BadRequest(new ApiResponse<object>(false, null, "Cart quantity exceeds available stock."));
            }

            existingItem.Quantity = newQty;
            existingItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, existingItem, "Product quantity updated in cart."));
        }

        var item = new CartItem
        {
            UserId = userId,
            GuestSessionId = guestSessionId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, item, "Product added to cart."));
    }

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCartItem(int productId, [FromBody] UpdateCartItemRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guestSessionId = GetGuestSessionId();

        var item = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.ProductId == productId &&
                ((userId != null && ci.UserId == userId) || (!string.IsNullOrWhiteSpace(guestSessionId) && ci.GuestSessionId == guestSessionId)));

        if (item is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Cart item not found."));
        }

        var nextQuantity = item.Quantity + request.Delta;
        if (nextQuantity <= 0)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new ApiResponse<object>(true, null, "Cart item removed."));
        }

        if (item.Product is not null && nextQuantity > item.Product.Stock)
        {
            return BadRequest(new ApiResponse<object>(false, null, "Requested quantity exceeds available stock."));
        }

        item.Quantity = nextQuantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, item, "Cart item updated."));
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFromCart(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guestSessionId = GetGuestSessionId();

        var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.ProductId == productId &&
            ((userId != null && ci.UserId == userId) || (!string.IsNullOrWhiteSpace(guestSessionId) && ci.GuestSessionId == guestSessionId)));

        if (item is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Cart item not found."));
        }

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, null, "Cart item removed."));
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guestSessionId = GetGuestSessionId();

        var items = await _context.CartItems
            .Where(ci => (userId != null && ci.UserId == userId) || (!string.IsNullOrWhiteSpace(guestSessionId) && ci.GuestSessionId == guestSessionId))
            .ToListAsync();

        if (items.Count == 0)
        {
            return Ok(new ApiResponse<object>(true, null, "Cart already empty."));
        }

        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, null, "Cart cleared successfully."));
    }

    [AllowAnonymous]
    [HttpPost("shipping/calculate")]
    public ActionResult<ApiResponse<ShippingCalculateResponseDto>> CalculateShipping([FromBody] ShippingCalculateRequestDto request)
    {
        var shippingFee = request.CartAmount >= 400000m ? 0m : 35000m;
        var isFreeShipping = shippingFee == 0m;

        return Ok(new ApiResponse<ShippingCalculateResponseDto>(
            true,
            new ShippingCalculateResponseDto(shippingFee, isFreeShipping, 400000m, Math.Max(0m, 400000m - request.CartAmount)),
            "Shipping fee calculated successfully."));
    }

    private string? GetGuestSessionId()
    {
        if (Request.Headers.TryGetValue("X-Session-ID", out var value))
        {
            return value.ToString();
        }

        return null;
    }
}
