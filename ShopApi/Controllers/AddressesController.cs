using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/shop/addresses")]
public sealed class AddressesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AddressesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var rows = await _db.CustomerAddresses.AsNoTracking().Where(a => a.UserId == userId).OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync(ct);
        return Ok(new { success = true, data = rows, message = "Addresses retrieved." });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressInput input, CancellationToken ct)
    {
        if (!Valid(input)) return BadRequest(Failure("INVALID_ADDRESS", "All recipient and address fields are required."));
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var address = new CustomerAddress { UserId = userId, Title = input.Title.Trim(), RecipientName = input.RecipientName.Trim(), Phone = input.Phone.Trim(), Province = input.Province.Trim(), City = input.City.Trim(), PostalAddress = input.PostalAddress.Trim(), PostalCode = input.PostalCode.Trim(), IsDefault = input.IsDefault };
        if (address.IsDefault) await ClearDefault(userId, ct);
        _db.CustomerAddresses.Add(address); await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = address.Id }, new { success = true, data = address });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AddressInput input, CancellationToken ct)
    {
        if (!Valid(input)) return BadRequest(Failure("INVALID_ADDRESS", "All recipient and address fields are required."));
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (address is null) return NotFound(Failure("ADDRESS_NOT_FOUND", "Address not found."));
        if (input.IsDefault) await ClearDefault(userId, ct);
        address.Title = input.Title.Trim(); address.RecipientName = input.RecipientName.Trim(); address.Phone = input.Phone.Trim(); address.Province = input.Province.Trim(); address.City = input.City.Trim(); address.PostalAddress = input.PostalAddress.Trim(); address.PostalCode = input.PostalCode.Trim(); address.IsDefault = input.IsDefault; address.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return Ok(new { success = true, data = address, message = "Address updated." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (address is null) return NotFound(Failure("ADDRESS_NOT_FOUND", "Address not found."));
        _db.CustomerAddresses.Remove(address); await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Address deleted." });
    }

    [HttpPut("{id:int}/default")]
    public async Task<IActionResult> SetDefault(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (address is null) return NotFound(Failure("ADDRESS_NOT_FOUND", "Address not found."));
        await ClearDefault(userId, ct); address.IsDefault = true; address.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct); return Ok(new { success = true, data = address, message = "Default address updated." });
    }

    private async Task ClearDefault(string userId, CancellationToken ct)
    {
        var current = await _db.CustomerAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync(ct);
        foreach (var address in current) address.IsDefault = false;
    }
    private static bool Valid(AddressInput x) => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.RecipientName) && !string.IsNullOrWhiteSpace(x.Phone) && !string.IsNullOrWhiteSpace(x.Province) && !string.IsNullOrWhiteSpace(x.City) && !string.IsNullOrWhiteSpace(x.PostalAddress) && !string.IsNullOrWhiteSpace(x.PostalCode);
    private static object Failure(string code, string message) => new { success = false, error = new { code, message } };
}

public sealed record AddressInput(string Title, string RecipientName, string Phone, string Province, string City, string PostalAddress, string PostalCode, bool IsDefault = false);
