# Shop API - Code First

ASP.NET Core 8 e-commerce API for cultural products, Quran and Tadabor books.

## Current API surface

- Public catalog: `GET /api/v1/shop/products`, `GET /api/v1/shop/products/{id}`, `GET /api/v1/shop/categories`
- Guest/member cart: `/api/v1/shop/cart` (guests must send `X-Session-ID`)
- Shipping calculation: `POST /api/v1/shop/shipping/calculate`
- Authenticated checkout and order history: `/api/v1/shop/orders`
- Customer address book: `GET|POST /api/v1/shop/addresses`, `PUT|DELETE /api/v1/shop/addresses/{id}`, `PUT /api/v1/shop/addresses/{id}/default`
- Invoice data: `GET /api/v1/shop/orders/{orderId}/invoice`
- Cancellation/refund requests: `POST /api/v1/shop/orders/{orderId}/cancel` and `/refund-requests`
- Admin shipment/status updates: `PUT /api/v1/shop/orders/{orderId}/shipment` and `/status`
- Payment request/verification: `/api/v1/shop/payment`

## Payment, shipping and database configuration

Payment requests are deliberately disabled outside Development until a real bank provider and credentials are configured. Do not treat callback query parameters as proof of payment. The included development mock is for local testing only. Carrier tracking only stores/displays the carrier code; live status requires a configured postal carrier API. Refund requests are queued for administrative review and are not an automatic bank refund.

The database schema has changed (customer addresses, payment records, coupons, refund requests, and coupon code on orders). Create and commit an EF Core migration before deploying/updating an existing database:

```bash
dotnet restore
dotnet build
dotnet ef migrations add CompleteShopFeatures --project ShopApi
dotnet ef database update --project ShopApi
```

Configure production secrets using environment variables or a secret store; never commit real JWT or payment credentials. Run with `dotnet run --project ShopApi`.
