using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application;
using VisitorGuide.Application.Services;
using Microsoft.EntityFrameworkCore;
using VisitorGuide.Infrastructure;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/landmarks")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DiemThamQuanController(
    IVisitorGuideService guideService,
    VisitorGuideDbContext dbContext) : ControllerBase
{
    private bool IsMaintenance(string id)
    {
        var key = id?.Trim();
        return !string.IsNullOrWhiteSpace(key)
            && dbContext.Pois.AsNoTracking().Any(p =>
                p.Slug == key && (p.Status == "Bảo trì" || p.Status == "Đang bảo trì"));
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DiaDiemThamQuanDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<DiaDiemThamQuanDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string language = "vi")
    {
        try
        {
            var poiInfo = dbContext.Pois
                .AsNoTracking()
                .Select(p => new { p.Slug, p.Status, p.DisplayOrder })
                .ToList();
            var order = poiInfo.ToDictionary(p => p.Slug, p => p.DisplayOrder, StringComparer.OrdinalIgnoreCase);
            var hidden = poiInfo
                .Where(p => p.Status == "Bảo trì" || p.Status == "Đang bảo trì")
                .Select(p => p.Slug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var danhSach = guideService.GetDanhSachDiaDiem(search, category, language)
                .Where(d => !hidden.Contains(d.Id))
                .OrderBy(d => order.TryGetValue(d.Id, out var position) ? position : 9999)
                .ToList();

            return Ok(danhSach);
        }
        catch (ArgumentException)
        {
            return UnsupportedLanguage();
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType<DiaDiemThamQuanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DiaDiemThamQuanDto> GetById(string id, [FromQuery] string language = "vi")
    {
        if (IsMaintenance(id))
        {
            return NotFound();
        }

        try
        {
            var diaDiem = guideService.GetDiaDiem(id, language);
            return diaDiem is null ? NotFound() : Ok(diaDiem);
        }
        catch (ArgumentException)
        {
            return UnsupportedLanguage();
        }
    }

    [HttpGet("{id}/details")]
    [ProducesResponseType<ChiTietDiaDiemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ChiTietDiaDiemDto> GetDetails(
        string id,
        [FromQuery] string language,
        [FromServices] XemChiTiet xemChiTiet)
    {
        if (IsMaintenance(id))
        {
            return NotFound();
        }

        try
        {
            var details = xemChiTiet.ThucHien(id, language);
            return details is null ? NotFound() : Ok(details);
        }
        catch (ArgumentException)
        {
            return UnsupportedLanguage();
        }
    }

    [HttpPost("{id}/location")]
    [ProducesResponseType<ViTriDiaDiemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ViTriDiaDiemDto> FindLocation(
        string id,
        [FromBody] TimViTriRequest request,
        [FromServices] TimViTri timViTri)
    {
        if (IsMaintenance(id))
        {
            return NotFound();
        }

        try
        {
            var location = timViTri.ThucHien(id, request.ViDo, request.KinhDo);
            return location is null ? NotFound() : Ok(location);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tọa độ vị trí không hợp lệ.",
                Detail = "Vĩ độ và kinh độ phải được cung cấp cùng nhau và nằm trong phạm vi hợp lệ.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private ActionResult UnsupportedLanguage() =>
        BadRequest(new ProblemDetails
        {
            Title = "Ngôn ngữ không được hỗ trợ.",
            Detail = "Chọn một trong các ngôn ngữ: vi, en, fr.",
            Status = StatusCodes.Status400BadRequest
        });

    public sealed record TimViTriRequest(double? ViDo, double? KinhDo);
}

[ApiController]
[Route("api/languages")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class NgonNguController(ChonNgonNgu chonNgonNgu) : ControllerBase
{
    [HttpGet("{language}")]
    [ProducesResponseType<NgonNguThuyetMinh>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<NgonNguThuyetMinh> Select(string language)
    {
        try
        {
            return Ok(chonNgonNgu.ThucHien(language));
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ.",
                Detail = "Chọn một trong các ngôn ngữ: vi, en, fr.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}