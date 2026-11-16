using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VisitorGuide.Api.Controllers;

public sealed record AdminLoginRequest(string? Username, string? Password);

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string LocalDemoUsername = "Admin";
    private const string LocalDemoPassword = "123";

    [AllowAnonymous]
    [HttpGet("development-demo")]
    public IActionResult GetDevelopmentDemoCredentials()
    {
        if (!IsLocalDevelopmentDemoEnabled())
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] = "no-store";
        return Ok(new { username = LocalDemoUsername, password = LocalDemoPassword });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var configuredUsername = configuration["AdminAuth:Username"];
        var configuredPassword = configuration["AdminAuth:Password"];
        if (string.IsNullOrWhiteSpace(configuredUsername) && string.IsNullOrWhiteSpace(configuredPassword))
        {
            if (!IsLocalDevelopmentRequest())
            {
                return Problem("Admin credentials are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            configuredUsername = LocalDemoUsername;
            configuredPassword = LocalDemoPassword;
        }
        else if (string.IsNullOrWhiteSpace(configuredUsername) || string.IsNullOrWhiteSpace(configuredPassword))
        {
            return Problem("Admin credentials are not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (request.Username is null || request.Password is null
            || !FixedTimeEquals(request.Username, configuredUsername)
            || !FixedTimeEquals(request.Password, configuredPassword))
        {
            return Unauthorized();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, configuredUsername), new Claim(ClaimTypes.Role, "Admin")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private bool IsLocalDevelopmentDemoEnabled()
    {
        return IsLocalDevelopmentRequest()
            && string.IsNullOrWhiteSpace(configuration["AdminAuth:Username"])
            && string.IsNullOrWhiteSpace(configuration["AdminAuth:Password"]);
    }

    private bool IsLocalDevelopmentRequest()
    {
        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        var host = Request.Host.Host;
        var isLoopbackHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host, out var hostAddress) && IPAddress.IsLoopback(hostAddress);

        return environment.IsDevelopment()
            && remoteAddress is not null
            && IPAddress.IsLoopback(remoteAddress)
            && isLoopbackHost;
    }

    private static bool FixedTimeEquals(string supplied, string configured)
    {
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash);
    }
}
