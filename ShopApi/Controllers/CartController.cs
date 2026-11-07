using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 20;

        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(p =>
                p.Title.Contains(normalized) ||
                p.CategoryTitle.Contains(normalized) ||
                p.ShortDesc.Contains(normalized) ||
                p.AuthorOrPublisher != null && p.AuthorOrPublisher.Contains(normalized));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.Rating)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new ProductDto(
                p.Id,
                p.Title,
                p.Category,
                p.CategoryTitle,
                p.ShortDesc,
                p.FullDesc,
                p.Price,
                p.OriginalPrice,
                p.Badge,
                p.ImageUrl,
                p.AuthorOrPublisher,
                p.Pages,
                p.Rating,
                p.Stock,
                p.IsActive))
            .ToListAsync();

        return Ok(new ApiResponse<IEnumerable<ProductDto>>(
            true,
            items,
            "Products retrieved successfully.",
            new { total, page, limit }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new ApiResponse<ProductDto>(false, null, "Product not found."));
        }

        return Ok(new ApiResponse<ProductDto>(true, new ProductDto(
            product.Id,
            product.Title,
            product.Category,
            product.CategoryTitle,
            product.ShortDesc,
            product.FullDesc,
            product.Price,
            product.OriginalPrice,
            product.Badge,
            product.ImageUrl,
            product.AuthorOrPublisher,
            product.Pages,
            product.Rating,
            product.Stock,
            product.IsActive), "Product retrieved successfully."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ApiResponse<ProductDto>(false, null, "Title is required."));
        }

        var product = new Product
        {
            Title = request.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "books" : request.Category.Trim().ToLowerInvariant(),
            CategoryTitle = string.IsNullOrWhiteSpace(request.CategoryTitle) ? "کتب تدبر" : request.CategoryTitle.Trim(),
            ShortDesc = request.ShortDesc,
            FullDesc = request.FullDesc,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Badge = request.Badge,
            ImageUrl = request.ImageUrl,
            AuthorOrPublisher = request.AuthorOrPublisher,
            Pages = request.Pages,
            Rating = request.Rating,
            Stock = request.Stock,
            IsActive = request.IsActive,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<ProductDto>(true, new ProductDto(
            product.Id,
            product.Title,
            product.Category,
            product.CategoryTitle,
            product.ShortDesc,
            product.FullDesc,
            product.Price,
            product.OriginalPrice,
            product.Badge,
            product.ImageUrl,
            product.AuthorOrPublisher,
            product.Pages,
            product.Rating,
            product.Stock,
            product.IsActive), "Product created successfully."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] CreateProductRequestDto request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new ApiResponse<ProductDto>(false, null, "Product not found."));
        }

        product.Title = request.Title.Trim();
        product.Category = string.IsNullOrWhiteSpace(request.Category) ? product.Category : request.Category.Trim().ToLowerInvariant();
        product.CategoryTitle = string.IsNullOrWhiteSpace(request.CategoryTitle) ? product.CategoryTitle : request.CategoryTitle.Trim();
        product.ShortDesc = request.ShortDesc;
        product.FullDesc = request.FullDesc;
        product.Price = request.Price;
        product.OriginalPrice = request.OriginalPrice;
        product.Badge = request.Badge;
        product.ImageUrl = request.ImageUrl;
        product.AuthorOrPublisher = request.AuthorOrPublisher;
        product.Pages = request.Pages;
        product.Rating = request.Rating;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<ProductDto>(true, new ProductDto(
            product.Id,
            product.Title,
            product.Category,
            product.CategoryTitle,
            product.ShortDesc,
            product.FullDesc,
            product.Price,
            product.OriginalPrice,
            product.Badge,
            product.ImageUrl,
            product.AuthorOrPublisher,
            product.Pages,
            product.Rating,
            product.Stock,
            product.IsActive), "Product updated successfully."));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new ApiResponse<object>(false, null, "Product not found."));
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, null, "Product deleted successfully."));
    }
}
