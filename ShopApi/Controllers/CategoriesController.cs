using Microsoft.AspNetCore.Mvc;

namespace ShopApi.Controllers;

[ApiController]
[Route("api/v1/shop/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { success = true, data = new { status = "Healthy", timestamp = DateTime.UtcNow } });
}
