using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application;
using VisitorGuide.Application.Services;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/landmarks")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DiemThamQuanController(IVisitorGuideService guideService) : ControllerBase
{
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
            // Lọc bỏ các địa điểm đang "Bảo trì" VÀ xếp địa điểm mới tạo xuống cuối danh sách
            var danhSach = guideService.GetDanhSachDiaDiem(search, category, language)
                .Where(d => !AdminDiaDiemController.IsMaintenance(d.Id))
                .OrderBy(d => AdminDiaDiemController.GetOrderIndex(d.Id))
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
        if (AdminDiaDiemController.IsMaintenance(id))
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
        if (AdminDiaDiemController.IsMaintenance(id))
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
        if (AdminDiaDiemController.IsMaintenance(id))
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