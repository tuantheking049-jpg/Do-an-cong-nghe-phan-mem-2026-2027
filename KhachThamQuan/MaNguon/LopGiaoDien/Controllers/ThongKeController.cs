using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Api.Services;
using VisitorGuide.Infrastructure;

namespace VisitorGuide.Controllers;

/// <summary>Identifies a browser and tab for live online tracking.</summary>
public sealed record OnlineVisitorRequest(Guid VisitorId, Guid? BrowserId = null);

/// <summary>Reports the number of active visitor browser sessions.</summary>
public sealed record OnlineVisitorResponse(int LuotTruyCap);

[Route("api/thongke")]
[ApiController]
public class ThongKeController(
    SqlServerRelationalStore relationalStore,
    IOnlineVisitorTracker onlineVisitorTracker) : ControllerBase
{
    // Lưu số lượt nghe theo từng ID địa điểm (6 địa điểm mặc định tổng = 178)
    public static readonly ConcurrentDictionary<string, int> LuotNgheTheoId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cho-noi-nga-nam"] = 32,
        ["chua-doi"] = 31,
        ["chua-la-han"] = 35,
        ["chua-phat-hoc-2"] = 20,
        ["khu-can-cu-tinh-uy-soc-trang"] = 30,
        ["tan-hue-vien"] = 30
    };

    [HttpGet]
    public IActionResult GetThongKe()
    {
        return Ok(new
        {
            luotTruyCap = onlineVisitorTracker.GetOnlineCount(),
            luotNghe = LuotNgheTheoId.Values.Sum(),
            chiTietTheoId = LuotNgheTheoId
        });
    }

    [HttpPost("online/heartbeat")]
    [ProducesResponseType(typeof(OnlineVisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<OnlineVisitorResponse> Heartbeat([FromBody] OnlineVisitorRequest request)
    {
        if (request.VisitorId == Guid.Empty || request.BrowserId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid visitor ID",
                Detail = "VisitorId and BrowserId must be non-empty GUIDs."
            });
        }

        var browserId = request.BrowserId ?? request.VisitorId;
        return Ok(new OnlineVisitorResponse(onlineVisitorTracker.Heartbeat(browserId, request.VisitorId)));
    }

    [HttpPost("online/leave")]
    [ProducesResponseType(typeof(OnlineVisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<OnlineVisitorResponse> Leave([FromBody] OnlineVisitorRequest request)
    {
        if (request.VisitorId == Guid.Empty || request.BrowserId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid visitor ID",
                Detail = "VisitorId and BrowserId must be non-empty GUIDs."
            });
        }

        var browserId = request.BrowserId ?? request.VisitorId;
        return Ok(new OnlineVisitorResponse(onlineVisitorTracker.Leave(browserId, request.VisitorId)));
    }

    [HttpPost("tang-nghe/{id}")]
    public async Task<IActionResult> TangNghe(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        string key = id.Trim().ToLowerInvariant();
        var plays = await relationalStore.IncrementPlayCountAsync(key, cancellationToken);
        if (plays is null) return NotFound();
        LuotNgheTheoId[key] = plays.Value;

        return Ok(new { id = key, plays = plays.Value });
    }
}