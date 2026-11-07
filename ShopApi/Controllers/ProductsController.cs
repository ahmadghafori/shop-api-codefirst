using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.DTOs;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetCategories()
    {
        var categories = await _context.Products
            .Select(p => new { p.Category, p.CategoryTitle })
            .Distinct()
            .OrderBy(x => x.Category)
            .Select(x => new CategoryDto(0, x.CategoryTitle, x.Category))
            .ToListAsync();

        if (categories.Count == 0)
        {
            categories = new List<CategoryDto>
            {
                new(1, "کتب تدبر", "books"),
                new(2, "قرآن ک��یم", "quran"),
                new(3, "محصولات فرهنگی", "cultural"),
                new(4, "کارت هدیه", "cards")
            };
        }

        return Ok(new ApiResponse<IEnumerable<CategoryDto>>(true, categories, "Categories loaded successfully."));
    }
}
