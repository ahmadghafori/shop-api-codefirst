using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopApi.Data;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["UserManagement:JwtKey"]
    ?? builder.Configuration["Jwt:Key"]
    ?? "N3xT_L3v3l_JWT_Secret_Key_This_Is_Very_Long_And_Secure_1234567890!@#$%^&*()";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var principal = context.Principal;
            if (principal is null)
                return Task.CompletedTask;

            var identity = principal.Identity as ClaimsIdentity;
            if (identity is null)
                return Task.CompletedTask;

            var userId = principal.FindFirstValue("ASI_UserID") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = principal.FindFirstValue("ASI_UserName") ?? principal.FindFirstValue(ClaimTypes.Name);

            if (!string.IsNullOrWhiteSpace(userId) && identity.FindFirst(ClaimTypes.NameIdentifier) is null)
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

            if (!string.IsNullOrWhiteSpace(userName) && identity.FindFirst(ClaimTypes.Name) is null)
                identity.AddClaim(new Claim(ClaimTypes.Name, userName));

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Keep Swagger enabled in all environments, including Release/production builds.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
