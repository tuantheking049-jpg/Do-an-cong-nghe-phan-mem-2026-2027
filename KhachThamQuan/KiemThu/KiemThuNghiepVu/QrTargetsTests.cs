using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VisitorGuide.Api.Controllers;

namespace VisitorGuide.Application.Tests;

public sealed class QrTargetsTests
{
    [Fact]
    public void GetQrTargetsUsesConfiguredPublicBaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicBaseUrl"] = "https://example.test/visitor?old=query#fragment"
            })
            .Build();
        var controller = new AdminDiaDiemController(null!, null!);

        var result = Assert.IsType<OkObjectResult>(controller.GetQrTargets(null!, configuration));
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));

        Assert.Equal(
            "https://example.test/visitor/",
            payload.RootElement.GetProperty("targets")[0].GetProperty("url").GetString());
    }
}
