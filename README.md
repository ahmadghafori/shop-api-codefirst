# Shop API - Code First

این پروژه یک API فروشگاه برای محصولات فرهنگی و کتب تدبر است که بر اساس مستند فنی شما طراحی شده است.

## ویژگی‌ها

- ASP.NET Core 8 Web API
- EF Core Code First
- SQL Server / LocalDB
- ASP.NET Core Identity
- JWT Authentication
- Swagger
- فروشگاه محصولات فرهنگی، قرآن و کتب تدبر
- سبد خرید برای کاربر و مهمان
- ثبت سفارش و محاسبه هزینه ارسال
- درگاه پرداخت mock

## مسیرهای اصلی

- `/api/v1/shop/products`
- `/api/v1/shop/categories`
- `/api/v1/shop/cart`
- `/api/v1/shop/orders`
- `/api/v1/shop/payment`
- `/api/v1/shop/shipping`
- `/api/v1/shop/auth`

## اجرای پروژه

```bash
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

## حساب مدیر پیش‌فرض

- Email: `admin@shopapi.com`
- Password: `Admin@123`

## نکته

این نسخه با مستند فنی فروشگاه محصولات فرهنگی و کتب تدبر هم‌راستا شده است و از الگوی `/api/v1/shop` پیروی می‌کند.
