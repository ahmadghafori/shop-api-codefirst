using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;

namespace ShopApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/shop/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => new { p.Category, p.CategoryTitle })
            .Select(g => new { id = 0, title = g.Key.CategoryTitle, slug = g.Key.Category })
            .OrderBy(c => c.slug)
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<object>(true, categories, "Categories loaded successfully."));
    }
}
