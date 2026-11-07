using ShopApi.Models;

namespace ShopApi.DTOs;

public record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(
    string UserId,
    string Email,
    string FullName,
    IEnumerable<string> Roles,
    string Token,
    DateTime ExpiresAt);

public record CategoryDto(int Id, string Name, string? Description);

public record ProductDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsFeatured,
    string ImageUrl,
    int CategoryId,
    string CategoryName);

public record CreateProductRequestDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsFeatured,
    string ImageUrl,
    int CategoryId);

public record AddCartItemRequestDto(int ProductId, int Quantity);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Total);

public record CheckoutRequestDto(string ShippingAddress, string PhoneNumber);

public record OrderItemDto(int Id, string ProductName, int Quantity, decimal Price);

public record OrderDto(
    int Id,
    string OrderNumber,
    string Status,
    DateTime OrderDate,
    decimal Total,
    string ShippingAddress,
    string PhoneNumber,
    IEnumerable<OrderItemDto> Items);
