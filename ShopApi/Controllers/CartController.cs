using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/shop/cart")]
public sealed class CartController : ControllerBase
{
    private const decimal FreeShippingThreshold = 400000m;
    private const decimal StandardShippingFee = 35000m;
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        if (!TryGetCartOwner(out var userId, out var guestId, out var error)) return error!;
        var rows = await CartQuery(userId, guestId).AsNoTracking().ToListAsync(ct);
        var items = rows.Select(x => new { productId = x.ProductId, quantity = x.Quantity,
            product = new { x.Product!.Id, x.Product.Title, x.Product.Category, x.Product.CategoryTitle,
                desc = x.Product.ShortDesc, fullDesc = x.Product.FullDesc, x.Product.Price,
                x.Product.OriginalPrice, x.Product.Badge, image = x.Product.ImageUrl,
                x.Product.AuthorOrPublisher, x.Product.Pages, x.Product.Rating, x.Product.Stock, x.Product.IsActive },
            unitPrice = x.Product!.Price, totalPrice = x.Product.Price * x.Quantity }).ToList();
        var current = items.Sum(x => x.totalPrice);
        var regular = rows.Sum(x => x.Product!.OriginalPrice * x.Quantity);
        var discount = Math.Max(0m, regular - current);
        var shipping = current >= FreeShippingThreshold ? 0m : StandardShippingFee;
        return Ok(new { success = true, data = new { items, summary = new {
            itemsCount = items.Sum(x => x.quantity), itemsPrice = regular, discountAmount = discount,
            shippingFee = shipping, totalPayable = current + shipping } }, message = "Cart loaded successfully." });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] CartItemInput request, CancellationToken ct)
    {
        if (!TryGetCartOwner(out var userId, out var guestId, out var error)) return error!;
        if (request.ProductId <= 0 || request.Quantity <= 0) return BadRequest(Failure("INVALID_INPUT", "Product ID and positive quantity are required."));
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, ct);
        if (product is null) return NotFound(Failure("PRODUCT_NOT_FOUND", "Product not found."));
        var item = await CartQuery(userId, guestId).FirstOrDefaultAsync(x => x.ProductId == request.ProductId, ct);
        if ((item?.Quantity ?? 0) + request.Quantity > product.Stock) return Conflict(Failure("OUT_OF_STOCK", "Requested quantity exceeds available stock."));
        if (item is null) _db.CartItems.Add(item = new CartItem { ProductId = product.Id, UserId = userId, GuestSessionId = guestId, Quantity = request.Quantity });
        else { item.Quantity += request.Quantity; item.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = new { productId = item.ProductId, quantity = item.Quantity }, message = "Product added to cart." });
    }

    [HttpPut("items/{productId:int}")]
    public async Task<IActionResult> UpdateItem(int productId, [FromBody] CartDeltaInput request, CancellationToken ct)
    {
        if (!TryGetCartOwner(out var userId, out var guestId, out var error)) return error!;
        var item = await CartQuery(userId, guestId).Include(x => x.Product).FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        if (item is null) return NotFound(Failure("CART_ITEM_NOT_FOUND", "Cart item not found."));
        var next = item.Quantity + request.Delta;
        if (next <= 0) _db.CartItems.Remove(item);
        else if (item.Product is null || next > item.Product.Stock) return Conflict(Failure("OUT_OF_STOCK", "Requested quantity exceeds available stock."));
        else { item.Quantity = next; item.UpdatedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = next <= 0 ? null : new { productId, quantity = next }, message = "Cart updated." });
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId, CancellationToken ct)
    {
        if (!TryGetCartOwner(out var userId, out var guestId, out var error)) return error!;
        var item = await CartQuery(userId, guestId).FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        if (item is null) return NotFound(Failure("CART_ITEM_NOT_FOUND", "Cart item not found."));
        _db.CartItems.Remove(item); await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = (object?)null, message = "Cart item removed." });
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        if (!TryGetCartOwner(out var userId, out var guestId, out var error)) return error!;
        var items = await CartQuery(userId, guestId).ToListAsync(ct);
        _db.CartItems.RemoveRange(items); await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, data = (object?)null, message = "Cart cleared." });
    }

    private IQueryable<CartItem> CartQuery(string? userId, string? guestId) => _db.CartItems.Include(x => x.Product)
        .Where(x => userId != null ? x.UserId == userId : x.GuestSessionId == guestId);

    private bool TryGetCartOwner(out string? userId, out string? guestId, out ObjectResult? error)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        guestId = null; error = null;
        if (!string.IsNullOrWhiteSpace(userId)) return true;
        guestId = Request.Headers["X-Session-ID"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(guestId) || guestId.Length > 150)
        { error = BadRequest(Failure("SESSION_REQUIRED", "Provide a valid X-Session-ID for guest cart access.")); return false; }
        return true;
    }

    private static object Failure(string code, string message) => new { success = false, error = new { code, message, details = (object?)null } };
}

public sealed record CartItemInput(int ProductId, int Quantity);
public sealed record CartDeltaInput(int Delta);
