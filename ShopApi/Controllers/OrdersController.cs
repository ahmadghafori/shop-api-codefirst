using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var cartItems = await _context.CartItems
            .Where(ci => ci.UserId == userId)
            .Include(ci => ci.Product)
            .Select(ci => new CartItemDto(
                ci.Id,
                ci.ProductId,
                ci.Product!.Name,
                ci.Product.Price,
                ci.Quantity,
                ci.Product.Price * ci.Quantity))
            .ToListAsync();

        return Ok(cartItems);
    }

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> AddToCart([FromBody] AddCartItemRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product is null)
        {
            return NotFound("Product not found.");
        }

        if (request.Quantity > product.StockQuantity)
        {
            return BadRequest("Requested quantity exceeds stock availability.");
        }

        var existingCartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);

        if (existingCartItem is not null)
        {
            existingCartItem.Quantity += request.Quantity;
            if (existingCartItem.Quantity > product.StockQuantity)
            {
                return BadRequest("Cart quantity exceeds product stock.");
            }

            await _context.SaveChangesAsync();
            return Ok(new CartItemDto(
                existingCartItem.Id,
                product.Id,
                product.Name,
                product.Price,
                existingCartItem.Quantity,
                product.Price * existingCartItem.Quantity));
        }

        var newCartItem = new CartItem
        {
            UserId = userId,
            ProductId = request.ProductId,
            Quantity = request.Quantity
        };

        _context.CartItems.Add(newCartItem);
        await _context.SaveChangesAsync();

        return Ok(new CartItemDto(
            newCartItem.Id,
            product.Id,
            product.Name,
            product.Price,
            newCartItem.Quantity,
            product.Price * newCartItem.Quantity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CartItemDto>> UpdateCartItem(int id, [FromBody] AddCartItemRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var cartItem = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

        if (cartItem is null)
        {
            return NotFound();
        }

        if (request.Quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        if (request.Quantity > cartItem.Product!.StockQuantity)
        {
            return BadRequest("Requested quantity exceeds stock availability.");
        }

        cartItem.Quantity = request.Quantity;
        await _context.SaveChangesAsync();

        return Ok(new CartItemDto(
            cartItem.Id,
            cartItem.ProductId,
            cartItem.Product.Name,
            cartItem.Product.Price,
            cartItem.Quantity,
            cartItem.Product.Price * cartItem.Quantity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == userId);

        if (cartItem is null)
        {
            return NotFound();
        }

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
