using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopApi.DTOs;
using ShopApi.Models;
using ShopApi.Services;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiResponse<AuthResponseDto>(false, null, "Email and password are required."));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(new ApiResponse<AuthResponseDto>(false, null, "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiResponse<AuthResponseDto>(false, null, string.Join(", ", result.Errors.Select(e => e.Description))));
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        return Ok(new ApiResponse<AuthResponseDto>(true, await BuildAuthResponseAsync(user), "User registered successfully."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ApiResponse<AuthResponseDto>(false, null, "Invalid email or password."));
        }

        var passwordValid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!passwordValid.Succeeded)
        {
            return Unauthorized(new ApiResponse<AuthResponseDto>(false, null, "Invalid email or password."));
        }

        return Ok(new ApiResponse<AuthResponseDto>(true, await BuildAuthResponseAsync(user), "Login successful."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized(new ApiResponse<AuthResponseDto>(false, null, "Unauthorized."));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new ApiResponse<AuthResponseDto>(false, null, "User not found."));
        }

        return Ok(new ApiResponse<AuthResponseDto>(true, await BuildAuthResponseAsync(user), "Current user profile."));
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateToken(user, roles);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponseDto(
            user.Id,
            user.Email ?? string.Empty,
            $"{user.FirstName} {user.LastName}".Trim(),
            roles,
            token,
            expiresAt);
    }
}
