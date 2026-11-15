using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application.Services;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/narration")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ThuyetMinhController(
    NgheNoiDung ngheNoiDung,
    TamDung tamDung,
    NgheLai ngheLai,
    VisitorGuide.Infrastructure.VisitorGuideDbContext dbContext) : ControllerBase
{
    private static bool IsBuiltInLang(string lang) =>
        lang.Equals("vi", StringComparison.OrdinalIgnoreCase) ||
        lang.Equals("en", StringComparison.OrdinalIgnoreCase) ||
        lang.Equals("fr", StringComparison.OrdinalIgnoreCase);

    [HttpGet("{id}")]
    [ProducesResponseType<NoiDungThuyetMinhDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<NoiDungThuyetMinhDto> GetNarration(
        string id,
        [FromQuery] string language = "vi")
    {
        if (AdminDiaDiemController.IsMaintenance(id)) return NotFound();
        if (!AdminDiaDiemController.IsLanguageSupported(language))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ hoặc đang tạm ngưng.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            string baseLang = IsBuiltInLang(language) ? language : "vi";
            var narration = ngheNoiDung.ThucHien(id, baseLang);
            if (narration is null) return NotFound();

            string description = narration.DiaDiem.Description;

            if (AdminDiaDiemController.IsTranslationDraft(id, language))
            {
                description = "(Nội dung thuyết minh đang chờ công bố)";
            }
            else if (!IsBuiltInLang(language))
            {
                var row = dbContext.MoTaDiaDiem
                    .FirstOrDefault(m => m.DiaDiemId == id && m.LanguageCode.ToLower() == language.ToLower());
                if (row != null && !string.IsNullOrWhiteSpace(row.Description) && row.Description != "(Chưa có bản dịch)")
                {
                    description = row.Description;
                }
            }

            var langInfo = AdminDiaDiemController.GetLanguageInfo(language);
            string voiceLocale = langInfo?.VoiceLocale ?? narration.MaGiongDoc;
            if (voiceLocale.StartsWith("jp", StringComparison.OrdinalIgnoreCase)) voiceLocale = "ja-JP";
            if (voiceLocale.StartsWith("kr", StringComparison.OrdinalIgnoreCase)) voiceLocale = "ko-KR";
            if (voiceLocale.StartsWith("cn", StringComparison.OrdinalIgnoreCase)) voiceLocale = "zh-CN";

            narration = narration with
            {
                DiaDiem = narration.DiaDiem with { Description = description },
                MaGiongDoc = voiceLocale
            };

            return Ok(narration);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("pause")]
    [ProducesResponseType<ViTriThuyetMinhDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ViTriThuyetMinhDto> Pause([FromBody] TamDungRequest request)
    {
        try
        {
            return Ok(tamDung.ThucHien(request.ViTriKyTu, request.DoDaiNoiDung));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Độ dài nội dung không hợp lệ.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("replay")]
    [ProducesResponseType<ViTriThuyetMinhDto>(StatusCodes.Status200OK)]
    public ActionResult<ViTriThuyetMinhDto> Replay() => Ok(ngheLai.ThucHien());

    public sealed record TamDungRequest(int ViTriKyTu, int DoDaiNoiDung);
}
