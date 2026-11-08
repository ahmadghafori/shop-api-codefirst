using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.DTOs;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop")]
public class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("auth/login")]
    public ActionResult<ApiResponse<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<AuthResponseDto>(
            false,
            null,
            "Authentication is delegated to UserManagement. Please sign in through the UserManagement service and send the resulting JWT to this API as a Bearer token."));
    }

    [AllowAnonymous]
    [HttpPost("auth/register")]
    public ActionResult<ApiResponse<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<AuthResponseDto>(
            false,
            null,
            "User registration is handled by UserManagement. ShopApi does not create local users."));
    }

    [Authorize]
    [HttpGet("auth/me")]
    public ActionResult<ApiResponse<AuthResponseDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("ASI_UserID");
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("ASI_UserName") ?? "user";

        return Ok(new ApiResponse<AuthResponseDto>(
            true,
            new AuthResponseDto(
                userId ?? string.Empty,
                userName,
                userName,
                new[] { "Customer" },
                string.Empty,
                DateTime.UtcNow.AddHours(24)),
            "Current user profile from UserManagement token."));
    }
}
