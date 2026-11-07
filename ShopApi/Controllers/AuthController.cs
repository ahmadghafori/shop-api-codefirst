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
    string Title,
    string Category,
    string CategoryTitle,
    string ShortDesc,
    string FullDesc,
    decimal Price,
    decimal OriginalPrice,
    string? Badge,
    string ImageUrl,
    string? AuthorOrPublisher,
    int? Pages,
    decimal Rating,
    int Stock,
    bool IsActive);

public record CreateProductRequestDto(
    string Title,
    string Category,
    string CategoryTitle,
    string ShortDesc,
    string FullDesc,
    decimal Price,
    decimal OriginalPrice,
    string? Badge,
    string ImageUrl,
    string? AuthorOrPublisher,
    int? Pages,
    decimal Rating,
    int Stock,
    bool IsActive = true);

public record AddCartItemRequestDto(int ProductId, int Quantity);

public record UpdateCartItemRequestDto(int Delta);

public record ShippingCalculateRequestDto(string Province, string City, decimal CartAmount);

public record ShippingCalculateResponseDto(
    decimal ShippingFee,
    bool IsFreeShipping,
    decimal FreeShippingThreshold,
    decimal RemainingForFreeShipping);

public record CheckoutRequestDto(
    CustomerInfo Customer,
    string PaymentMethod);

public record CustomerInfo(
    string FullName,
    string Phone,
    string Province,
    string City,
    string PostalAddress,
    string PostalCode,
    string? Notes);

public record CartItemDto(
    int ProductId,
    int Quantity,
    ProductDto Product,
    decimal UnitPrice,
    decimal TotalPrice);

public record CartSummaryDto(
    int ItemsCount,
    decimal ItemsPrice,
    decimal DiscountAmount,
    decimal ShippingFee,
    decimal TotalPayable);

public record OrderItemDto(int Id, int ProductId, string ProductTitle, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record OrderDto(
    string Id,
    string? TrackingNumber,
    string Status,
    DateTime CreatedAt,
    decimal TotalPayable,
    string CustomerName,
    string CustomerPhone,
    IEnumerable<OrderItemDto> Items);

public record PaymentRequestDto(string OrderId);

public record PaymentVerifyQueryDto(string Authority, string Status);

public record ApiResponse<T>(bool Success, T? Data, string Message, object? Meta = null);
