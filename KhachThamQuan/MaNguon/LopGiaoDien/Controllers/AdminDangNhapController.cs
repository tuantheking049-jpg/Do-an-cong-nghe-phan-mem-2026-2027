using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorGuide.Infrastructure;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminDangNhapController(VisitorGuideDbContext dbContext) : ControllerBase
{
    public sealed record DangNhapRequest(string? Username, string? Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] DangNhapRequest request)
    {
        var username = request.Username?.Trim();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin!" });
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password)));

        var admin = await dbContext.AdminAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Username == username);

        if (admin is null
            || admin.IsLocked
            || !string.Equals(admin.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
        }

        return Ok(new { fullName = admin.FullName });
    }
}
