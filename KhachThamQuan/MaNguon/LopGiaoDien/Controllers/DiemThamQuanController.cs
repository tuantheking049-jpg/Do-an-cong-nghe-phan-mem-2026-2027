using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application;
using VisitorGuide.Application.Services;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/landmarks")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DiemThamQuanController(
    IVisitorGuideService guideService,
    VisitorGuide.Infrastructure.VisitorGuideDbContext dbContext) : ControllerBase
{
    private static bool IsBuiltInLang(string lang) =>
        lang.Equals("vi", StringComparison.OrdinalIgnoreCase) ||
        lang.Equals("en", StringComparison.OrdinalIgnoreCase) ||
        lang.Equals("fr", StringComparison.OrdinalIgnoreCase);

    private string GetCustomLangText(string diaDiemId, string lang, string fallbackText)
    {
        var row = dbContext.MoTaDiaDiem
            .FirstOrDefault(m => m.DiaDiemId == diaDiemId && m.LanguageCode.ToLower() == lang.ToLower());
        return (row != null && !string.IsNullOrWhiteSpace(row.Description) && row.Description != "(Chưa có bản dịch)")
            ? row.Description
            : fallbackText;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DiaDiemThamQuanDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<DiaDiemThamQuanDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string language = "vi")
    {
        if (!AdminDiaDiemController.IsLanguageSupported(language))
        {
            return UnsupportedLanguage();
        }

        try
        {
            string baseLang = IsBuiltInLang(language) ? language : "vi";
            var danhSach = guideService.GetDanhSachDiaDiem(search, category, baseLang)
                .Where(d => !AdminDiaDiemController.IsMaintenance(d.Id))
                .OrderBy(d => AdminDiaDiemController.GetOrderIndex(d.Id))
                .Select(d =>
                {
                    if (AdminDiaDiemController.IsTranslationDraft(d.Id, language))
                        return d with { Description = "(Nội dung thuyết minh đang chờ công bố)" };

                    if (!IsBuiltInLang(language))
                        return d with { Description = GetCustomLangText(d.Id, language, d.Description) };

                    return d;
                })
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
        if (AdminDiaDiemController.IsMaintenance(id)) return NotFound();
        if (!AdminDiaDiemController.IsLanguageSupported(language)) return UnsupportedLanguage();

        try
        {
            string baseLang = IsBuiltInLang(language) ? language : "vi";
            var diaDiem = guideService.GetDiaDiem(id, baseLang);
            if (diaDiem is null) return NotFound();

            if (AdminDiaDiemController.IsTranslationDraft(id, language))
            {
                diaDiem = diaDiem with { Description = "(Nội dung thuyết minh đang chờ công bố)" };
            }
            else if (!IsBuiltInLang(language))
            {
                diaDiem = diaDiem with { Description = GetCustomLangText(id, language, diaDiem.Description) };
            }

            return Ok(diaDiem);
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
        if (AdminDiaDiemController.IsMaintenance(id)) return NotFound();
        if (!AdminDiaDiemController.IsLanguageSupported(language)) return UnsupportedLanguage();

        try
        {
            string baseLang = IsBuiltInLang(language) ? language : "vi";
            var details = xemChiTiet.ThucHien(id, baseLang);
            if (details is null) return NotFound();

            if (AdminDiaDiemController.IsTranslationDraft(id, language))
            {
                details = details with { NoiDung = "(Nội dung thuyết minh đang chờ công bố)" };
            }
            else if (!IsBuiltInLang(language))
            {
                details = details with { NoiDung = GetCustomLangText(id, language, details.NoiDung) };
            }

            return Ok(details);
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
        if (AdminDiaDiemController.IsMaintenance(id)) return NotFound();

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
            Title = "Ngôn ngữ không được hỗ trợ hoặc đang tạm ngưng.",
            Detail = "Vui lòng chọn một ngôn ngữ đang hoạt động.",
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
    public IActionResult Select(string language)
    {
        if (!AdminDiaDiemController.IsLanguageSupported(language))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ hoặc đang tạm ngưng.",
                Detail = "Ngôn ngữ này hiện không khả dụng.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            return Ok(chonNgonNgu.ThucHien(language));
        }
        catch (ArgumentException)
        {
            var info = AdminDiaDiemController.GetLanguageInfo(language);
            if (info == null) return BadRequest();

            return Ok(new
            {
                code = info.Code,
                language = info.Code,
                name = info.Name,
                displayName = info.Name,
                voiceLocale = info.VoiceLocale
            });
        }
    }
}