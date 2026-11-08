using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
                }
            }
        }

        var adminEmail = "admin@shopapi.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Shop",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create the seed admin user: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign the Admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await context.Products.AnyAsync())
        {
            var products = new[]
            {
                new Product
                {
                    Title = "مجموعه ۳ جلدی تدبر در قرآن کریم",
                    Category = "books",
                    CategoryTitle = "کتب تدبر",
                    ShortDesc = "دوره جامع آموزش روش‌ها و مقدمات تدبر مفهومی و ساختاری در آیات قرآن کریم.",
                    FullDesc = "این دوره مرجع شامل سه جلد آموزشی و کارگاهی است که با زبانی شیوا قواعد فهم ساختارمند قرآن را آموزش می‌دهد.",
                    Price = 360000m,
                    OriginalPrice = 450000m,
                    Badge = "پرفروش‌ترین",
                    ImageUrl = "/assets/shop/book_tadabor_set.svg",
                    AuthorOrPublisher = "انتشارات بینش وحیانی",
                    Pages = 640,
                    Rating = 4.9m,
                    Stock = 42,
                    IsActive = true
                },
                new Product
                {
                    Title = "تدبر در آیات محبت",
                    Category = "books",
                    CategoryTitle = "کتب تدبر",
                    ShortDesc = "بررسی عمیق مفاهیم محبت و ارتباط آن با زندگی معنوی.",
                    FullDesc = "کتاب تدبر در آیات محبت به صورت کاربردی، با رویکردی تفسیری و تربیتی ریشه‌های عشق، مهربانی و تعامل انسانی را بررسی می‌کند.",
                    Price = 180000m,
                    OriginalPrice = 220000m,
                    Badge = "جدید",
                    ImageUrl = "/assets/shop/book_love_quran.svg",
                    AuthorOrPublisher = "مؤسسه معنویت و فرهنگ",
                    Pages = 220,
                    Rating = 4.8m,
                    Stock = 28,
                    IsActive = true
                },
                new Product
                {
                    Title = "قرآن کریم با ترجمه روان",
                    Category = "quran",
                    CategoryTitle = "قرآن کریم",
                    ShortDesc = "نسخه ماندگار از قرآن کریم با ترجمه روان و خوانا.",
                    FullDesc = "این نسخه برای مطالعه‌ی روزمره و تفکر عمیق در آیات، با ترجمه روان و خوش‌خوان، مناسب خانواده و جوانان است.",
                    Price = 290000m,
                    OriginalPrice = 360000m,
                    Badge = "ارسال رایگان",
                    ImageUrl = "/assets/shop/quran_ravan.svg",
                    AuthorOrPublisher = "انتشارات نور الهی",
                    Pages = 720,
                    Rating = 4.9m,
                    Stock = 51,
                    IsActive = true
                },
                new Product
                {
                    Title = "کارت هدیه معنوی",
                    Category = "cards",
                    CategoryTitle = "کارت هدیه",
                    ShortDesc = "کارت هدیه برای مراسم و هدیه‌های معنوی و فرهنگی.",
                    FullDesc = "کارت هدیه مناسب برای دعوت‌های مذهبی، هدیه به دوستان و اعضای خانواده و مناسبت‌های خاص.",
                    Price = 120000m,
                    OriginalPrice = 150000m,
                    Badge = "پیشنهاد ویژه",
                    ImageUrl = "/assets/shop/gift_card.svg",
                    AuthorOrPublisher = "تدبر لايف",
                    Pages = null,
                    Rating = 4.7m,
                    Stock = 91,
                    IsActive = true
                },
                new Product
                {
                    Title = "پکیج فرهنگی خانواده",
                    Category = "cultural",
                    CategoryTitle = "محصولات فرهنگی",
                    ShortDesc = "مجموعه‌ای از ابزارها و محتوای فرهنگی برای رشد خانواده.",
                    FullDesc = "این بسته با هدف تقویت خانواده و فرهنگ زندگی معنوی طراحی شده و شامل ابزارهای آموزشی و فرهنگی برای همسران و فرزندان است.",
                    Price = 420000m,
                    OriginalPrice = 520000m,
                    Badge = "پک خانواده",
                    ImageUrl = "/assets/shop/family_cultural_pack.svg",
                    AuthorOrPublisher = "دفتر فرهنگی تدبر",
                    Pages = 340,
                    Rating = 4.8m,
                    Stock = 15,
                    IsActive = true
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}
