using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;

namespace ShopApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/shop/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1 || limit is < 1 or > 100)
            return BadRequest(new { success = false, error = new { code = "INVALID_PAGINATION", message = "page must be positive and limit must be between 1 and 100." } });

        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.Category == category.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Title.Contains(term) || p.CategoryTitle.Contains(term) ||
                p.ShortDesc.Contains(term) || p.FullDesc.Contains(term) ||
                (p.AuthorOrPublisher != null && p.AuthorOrPublisher.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.Id).Skip((page - 1) * limit).Take(limit)
            .Select(p => new { p.Id, p.Title, p.Category, p.CategoryTitle, desc = p.ShortDesc, fullDesc = p.FullDesc,
                p.Price, p.OriginalPrice, p.Badge, image = p.ImageUrl, p.AuthorOrPublisher, p.Pages,
                p.Rating, p.Stock, p.IsActive, p.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(new { success = true, data = items, message = "Products retrieved successfully.", meta = new { page, limit, total } });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (p is null) return NotFound(new { success = false, error = new { code = "PRODUCT_NOT_FOUND", message = "Product not found." } });
        return Ok(new { success = true, data = new { p.Id, p.Title, p.Category, p.CategoryTitle, desc = p.ShortDesc,
            fullDesc = p.FullDesc, p.Price, p.OriginalPrice, p.Badge, image = p.ImageUrl, p.AuthorOrPublisher,
            p.Pages, p.Rating, p.Stock, p.IsActive, p.CreatedAt }, message = "Product retrieved successfully." });
    }
}
