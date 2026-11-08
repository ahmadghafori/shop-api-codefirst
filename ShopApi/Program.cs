using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopApi.Data;
using ShopApi.Models;
using ShopApi.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
    var migrations = db.Database.GetMigrations().ToArray();

    logger.LogInformation(
        "Initializing ShopApi database {Database} on {DataSource}. Found {MigrationCount} EF migrations.",
        db.Database.GetDbConnection().Database,
        db.Database.GetDbConnection().DataSource,
        migrations.Length);

    if (migrations.Length > 0)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        // This creates the EF schema for a new/empty database when no migrations exist.
        await db.Database.EnsureCreatedAsync();
    }

    var rolesTableExists = await db.Database
        .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
        .SingleAsync();

    if (rolesTableExists == 0)
    {
        throw new InvalidOperationException(
            $"Database initialization did not create dbo.AspNetRoles. Connected database: " +
            $"'{db.Database.GetDbConnection().Database}' on '{db.Database.GetDbConnection().DataSource}'. " +
            "Check the active ConnectionStrings:DefaultConnection and add/apply an EF Core initial migration. " +
            "EnsureCreated cannot repair an existing database that already contains other tables.");
    }

    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
