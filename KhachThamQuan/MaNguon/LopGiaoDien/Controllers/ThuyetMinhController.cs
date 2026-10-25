using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application.Services;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/narration")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ThuyetMinhController(
    NgheNoiDung ngheNoiDung,
    TamDung tamDung,
    NgheLai ngheLai) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType<NoiDungThuyetMinhDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<NoiDungThuyetMinhDto> GetNarration(
        string id,
        [FromQuery] string language = "vi")
    {
        try
        {
            var narration = ngheNoiDung.ThucHien(id, language);
            return narration is null ? NotFound() : Ok(narration);
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
