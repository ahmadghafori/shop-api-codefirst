# Shop API - Code First

This repository contains a complete ASP.NET Core Web API for an e-commerce store built using:

- ASP.NET Core 8 Web API
- EF Core Code First
- SQL Server
- ASP.NET Core Identity
- JWT Authentication
- Swagger / OpenAPI
- Role-based authorization

## Features

- Product catalog
- Categories
- Customer cart
- Orders and order details
- Admin APIs
- JWT login and registration
- Seed data for admin and sample products

## Project structure

- `ShopApi/` – main API project
- `ShopApi/Controllers/` – API endpoints
- `ShopApi/Data/` – EF Core DbContext
- `ShopApi/Models/` – entities
- `ShopApi/Services/` – JWT token generation and seed logic

## Setup

1. Edit the connection string in `ShopApi/appsettings.json`.
2. Restore packages:

```bash
dotnet restore
```

3. Build the project:

```bash
dotnet build
```

4. Run the project:

```bash
dotnet run --project ShopApi
```

5. Open Swagger in the browser:

```text
https://localhost:5001/swagger
```

## Default admin account

- Email: `admin@shopapi.com`
- Password: `Admin@123`

## Notes

This project uses the Code First approach. After cloning, you can generate migrations with:

```bash
dotnet ef migrations add InitialCreate --project ShopApi --startup-project ShopApi
```

Then update the database:

```bash
dotnet ef database update --project ShopApi --startup-project ShopApi
```
