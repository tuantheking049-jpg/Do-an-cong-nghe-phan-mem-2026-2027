using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace VisitorGuide.Controllers;

[Route("api/thongke")]
[ApiController]
public class ThongKeController : ControllerBase
{
    private static int _soLuotTruyCap = 0;

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
            luotTruyCap = _soLuotTruyCap,
            luotNghe = LuotNgheTheoId.Values.Sum(),
            chiTietTheoId = LuotNgheTheoId
        });
    }

    [HttpPost("tang-truy-cap")]
    public IActionResult TangTruyCap()
    {
        _soLuotTruyCap++;
        return Ok(new { luotTruyCap = _soLuotTruyCap });
    }

    [HttpPost("giam-truy-cap")]
    public IActionResult GiamTruyCap()
    {
        if (_soLuotTruyCap > 0) _soLuotTruyCap--;
        return Ok();
    }

    [HttpPost("tang-nghe/{id}")]
    public IActionResult TangNghe(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        string key = id.Trim().ToLowerInvariant();
        LuotNgheTheoId.AddOrUpdate(key, 1, (_, current) => current + 1);

        return Ok(new { id = key, plays = LuotNgheTheoId[key] });
    }
}