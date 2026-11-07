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
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized(new ApiResponse<IEnumerable<OrderDto>>(false, null, "Unauthorized."));
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.TrackingNumber,
                o.Status,
                o.CreatedAt,
                o.TotalPayable,
                o.CustomerName,
                o.CustomerPhone,
                o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductTitle, i.Quantity, i.UnitPrice, i.TotalPrice))))
            .ToListAsync();

        return Ok(new ApiResponse<IEnumerable<OrderDto>>(true, orders, "Orders retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateOrder([FromBody] CheckoutRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized(new ApiResponse<object>(false, null, "Unauthorized."));
        }

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return BadRequest(new ApiResponse<object>(false, null, "Your cart is empty."));
        }

        foreach (var item in cartItems)
        {
            if (item.Product is null)
            {
                return BadRequest(new ApiResponse<object>(false, null, $"Product {item.ProductId} is no longer available."));
            }

            if (item.Quantity > item.Product.Stock)
            {
                return BadRequest(new ApiResponse<object>(false, null, $"Not enough stock available for {item.Product.Title}."));
            }
        }

        var orderId = $"TB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var trackingNumber = $"2048{DateTime.UtcNow:yyyyMMddHHmmss}";

        var order = new Order
        {
            Id = orderId,
            UserId = userId,
            CustomerName = request.Customer.FullName,
            CustomerPhone = request.Customer.Phone,
            Province = request.Customer.Province,
            City = request.Customer.City,
            PostalAddress = request.Customer.PostalAddress,
            PostalCode = request.Customer.PostalCode,
            Notes = request.Customer.Notes,
            PaymentMethod = request.PaymentMethod,
            Status = "pending_payment",
            TrackingNumber = trackingNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal itemsPrice = 0m;
        foreach (var item in cartItems)
        {
            var unitPrice = item.Product!.Price;
            var total = unitPrice * item.Quantity;
            itemsPrice += total;

            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductTitle = item.Product.Title,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = total
            });

            item.Product.Stock -= item.Quantity;
        }

        var shippingFee = itemsPrice >= 400000m ? 0m : 35000m;
        order.ItemsPrice = itemsPrice;
        order.DiscountAmount = 0m;
        order.ShippingFee = shippingFee;
        order.TotalPayable = itemsPrice + shippingFee;

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        return StatusCode(201, new ApiResponse<object>(true, new
        {
            orderId = order.Id,
            trackingNumber = order.TrackingNumber,
            totalPayable = order.TotalPayable,
            status = order.Status,
            paymentUrl = $"/api/v1/shop/payment/mock-gateway?orderId={order.Id}"
        }, "Order created successfully."));
    }

    [HttpGet("track/{trackingNumber}")]
    public async Task<ActionResult<ApiResponse<object>>> TrackOrder(string trackingNumber)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber);
        if (order is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Order not found."));
        }

        return Ok(new ApiResponse<object>(true, new
        {
            trackingNumber = order.TrackingNumber,
            status = order.Status,
            province = order.Province,
            city = order.City,
            updatedAt = order.UpdatedAt
        }, "Order status retrieved successfully."));
    }
}
