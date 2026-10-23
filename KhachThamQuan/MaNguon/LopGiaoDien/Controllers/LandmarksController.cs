using Microsoft.AspNetCore.Mvc;
using VisitorGuide.Application;
using VisitorGuide.Application.Services;

namespace VisitorGuide.Api.Controllers;

[ApiController]
[Route("api/landmarks")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LandmarksController(IVisitorGuideService guideService) : ControllerBase
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi", "en", "fr"
    };

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LandmarkDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<LandmarkDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string language = "vi")
    {
        if (!SupportedLanguages.Contains(language))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ.",
                Detail = "Chọn một trong các ngôn ngữ: vi, en, fr.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(guideService.GetLandmarks(search, category, language));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<LandmarkDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<LandmarkDto> GetById(string id, [FromQuery] string language = "vi")
    {
        if (!SupportedLanguages.Contains(language))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Ngôn ngữ không được hỗ trợ.",
                Detail = "Chọn một trong các ngôn ngữ: vi, en, fr.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var landmark = guideService.GetLandmark(id, language);
        return landmark is null ? NotFound() : Ok(landmark);
    }
}
