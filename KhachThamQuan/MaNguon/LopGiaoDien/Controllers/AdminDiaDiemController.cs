using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorGuide.Controllers;
using VisitorGuide.Infrastructure;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Api.Controllers;

public sealed record AdminDiaDiemRequest(
    string Id,
    string Name,
    string Category,
    string Province,
    string? Status,
    int? Plays
);

public sealed record AdminNarrationRequest(
    string Title,
    int DurationMinutes,
    string ImageUrl,
    string DescriptionVi,
    string HighlightsText,
    string PublishStatus
);

[ApiController]
[Route("api/admin/landmarks")]
public class AdminDiaDiemController(VisitorGuideDbContext dbContext) : ControllerBase
{
    private const string DefaultImageUrl = "https://images.vietnamtourism.gov.vn/vn/images/2014/Chonoinga5.jpg";
    private const string Draft = "Bản nháp";
    private const string Published = "Đã công bố";


    [HttpGet]
    public async Task<IActionResult> GetAllForAdmin()
    {
        await EnsureNarrationRowsAsync();

        var pois = await dbContext.Pois
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.PoiID)
            .ToListAsync();

        foreach (var poi in pois)
        {
            ThongKeController.LuotNgheTheoId.TryAdd(poi.Slug, 0);
        }

        var danhSach = pois
            .Select(p => new
            {
                id = p.Slug,
                name = p.Name,
                province = p.Province ?? "",
                category = p.Category ?? "",
                plays = ThongKeController.LuotNgheTheoId.TryGetValue(p.Slug, out var plays) ? plays : 0,
                status = p.Status,
                orderIndex = p.DisplayOrder
            })
            .ToList();

        return Ok(danhSach);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminDiaDiemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "ID và Tên địa điểm không được để trống!" });
        }

        var id = request.Id.Trim();
        if (await dbContext.Pois.AnyAsync(x => x.Slug == id))
        {
            return Conflict(new { message = $"Mã ID '{id}' đã tồn tại trong Database!" });
        }

        var name = request.Name.Trim();
        var category = string.IsNullOrWhiteSpace(request.Category) ? "Văn hóa" : request.Category.Trim();
        var province = string.IsNullOrWhiteSpace(request.Province) ? "Sóc Trăng" : request.Province.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Hoạt động" : request.Status.Trim();

        var nextOrder = (await dbContext.Pois.MaxAsync(p => (int?)p.DisplayOrder) ?? 0) + 1;

        var poi = new PoiEntity
        {
            Slug = id,
            Name = name,
            Category = category,
            Province = province,
            Status = status,
            DisplayOrder = nextOrder,
            DurationMinutes = 5,
            Rating = 0,
            ReviewCount = 0,
            ImageUrl = DefaultImageUrl,
            ImageSourceUrl = "https://soctrang.gov.vn",
            ImagePosition = "center center",
            HighlightsJson = PoiJson.WriteHighlights([$"Điểm tham quan {category}", $"Du lịch {province}"]),
            GalleryJson = PoiJson.WriteGallery(
            [
                new PhotoJson(DefaultImageUrl, "https://soctrang.gov.vn", $"Hình ảnh {name}")
            ]),
            Narration = new NarrationEntity
            {
                SourceLang = "vi",
                Title = $"Thuyết minh {name}",
                Content = null,
                PublishStatus = Draft,
                UpdatedAt = DateTime.Now,
                Translations =
                [
                    new TranslationEntity
                    {
                        LangCode = "en",
                        TranslatedContent = $"{name} is a featured {category} attraction located in {province}."
                    },
                    new TranslationEntity
                    {
                        LangCode = "fr",
                        TranslatedContent = $"{name} est un site remarquable ({category}) situé à {province}."
                    }
                ]
            }
        };

        dbContext.Pois.Add(poi);
        await dbContext.SaveChangesAsync();

        ThongKeController.LuotNgheTheoId[id] = request.Plays ?? 0;

        return Ok(new { message = "Thêm địa điểm mới vào Database thành công!", id = poi.Slug });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminDiaDiemRequest request)
    {
        var poi = await dbContext.Pois.FirstOrDefaultAsync(x => x.Slug == id);
        if (poi == null)
        {
            return NotFound(new { message = "Không tìm thấy địa điểm cần sửa!" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên địa điểm không được để trống!" });
        }

        poi.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Category)) poi.Category = request.Category.Trim();
        if (!string.IsNullOrWhiteSpace(request.Province)) poi.Province = request.Province.Trim();
        if (!string.IsNullOrWhiteSpace(request.Status)) poi.Status = request.Status.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Cập nhật thông tin vào Database thành công!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var poi = await dbContext.Pois.FirstOrDefaultAsync(x => x.Slug == id);
        if (poi == null)
        {
            return NotFound(new { message = "Không tìm thấy địa điểm cần xóa!" });
        }

        dbContext.Pois.Remove(poi);
        await dbContext.SaveChangesAsync();

        ThongKeController.LuotNgheTheoId.TryRemove(id, out _);

        return Ok(new { message = "Đã xóa địa điểm khỏi Database thành công!" });
    }


    [HttpGet("narrations")]
    public async Task<IActionResult> GetAllNarrations()
    {
        await EnsureNarrationRowsAsync();

        var pois = await dbContext.Pois
            .AsNoTracking()
            .Include(p => p.Narration)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.PoiID)
            .ToListAsync();

        var result = pois
            .Select(p =>
            {
                var narration = p.Narration;
                var content = narration?.Content;
                bool isEmpty = string.IsNullOrWhiteSpace(content) || content == PoiJson.EmptyNarrationPlaceholder;

                string publishStatus = narration?.PublishStatus == Published ? Published : Draft;
                string title = !string.IsNullOrWhiteSpace(narration?.Title)
                    ? narration!.Title
                    : $"Thuyết minh {p.Name}";

                var highlights = PoiJson.ReadHighlights(p.HighlightsJson);

                return new
                {
                    id = p.Slug,
                    name = p.Name,
                    category = p.Category ?? "",
                    title = title,
                    durationMinutes = p.DurationMinutes,
                    imageUrl = p.ImageUrl ?? "",
                    descriptionVi = isEmpty ? "" : content!,
                    highlights = highlights,
                    highlightsText = string.Join(", ", highlights),
                    publishStatus = publishStatus,
                    hasContent = !isEmpty,
                    orderIndex = p.DisplayOrder
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPut("{id}/narration")]
    public async Task<IActionResult> SaveNarration(string id, [FromBody] AdminNarrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.DescriptionVi))
        {
            return BadRequest(new { message = "Tiêu đề và Nội dung thuyết minh không được để trống (Luồng 5a)!" });
        }

        var poi = await dbContext.Pois
            .Include(p => p.Narration)
            .FirstOrDefaultAsync(p => p.Slug == id);

        if (poi == null)
        {
            return NotFound(new { message = "Địa điểm liên quan không tồn tại trong cơ sở dữ liệu (Luồng 6a)!" });
        }

        poi.DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 5;

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            poi.ImageUrl = request.ImageUrl.Trim();

            var gallery = PoiJson.ReadGallery(poi.GalleryJson);
            if (gallery.Count > 0)
            {
                gallery[0] = gallery[0] with { Url = poi.ImageUrl, Caption = $"Hình ảnh {poi.Name}" };
                poi.GalleryJson = PoiJson.WriteGallery(gallery);
            }
        }

        string cleanTitle = request.Title.Trim();
        string cleanContent = request.DescriptionVi.Trim();
        string pubStatus = (!string.IsNullOrWhiteSpace(request.PublishStatus) &&
                            request.PublishStatus.Trim().Equals(Draft, StringComparison.OrdinalIgnoreCase))
            ? Draft
            : Published;

        var narration = poi.Narration;
        if (narration == null)
        {
            narration = new NarrationEntity { SourceLang = "vi" };
            poi.Narration = narration;
        }

        narration.Title = cleanTitle;
        narration.Content = cleanContent;
        narration.PublishStatus = pubStatus;
        narration.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(request.HighlightsText))
        {
            var items = request.HighlightsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Length > 300 ? s[..300] : s)
                .Distinct()
                .ToList();

            if (items.Count > 0)
            {
                poi.HighlightsJson = PoiJson.WriteHighlights(items);
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { message = $"Đã lưu bài thuyết minh \"{cleanTitle}\" ở trạng thái [{pubStatus}] thành công!" });
    }

    [HttpDelete("{id}/narration")]
    public async Task<IActionResult> DeleteNarration(string id)
    {
        var narration = await dbContext.Narrations
            .Include(n => n.Poi)
            .FirstOrDefaultAsync(n => n.Poi.Slug == id);

        if (narration == null)
        {
            return NotFound(new { message = "Không tìm thấy bài thuyết minh cần xóa!" });
        }

        narration.Content = null;
        narration.Title = $"Thuyết minh {narration.Poi.Name}";
        narration.PublishStatus = Draft;
        narration.UpdatedAt = DateTime.Now;

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Đã xóa nội dung và chuyển về trạng thái Bản nháp!" });
    }

    private async Task EnsureNarrationRowsAsync()
    {
        var pois = await dbContext.Pois
            .Include(p => p.Narration)
                .ThenInclude(n => n!.Translations)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var poi in pois)
        {
            var narration = poi.Narration;
            if (narration == null)
            {
                narration = new NarrationEntity
                {
                    SourceLang = "vi",
                    Title = $"Thuyết minh {poi.Name}",
                    Content = null,
                    PublishStatus = Draft,
                    UpdatedAt = DateTime.Now
                };
                poi.Narration = narration;
            }

            var existingLangs = narration.Translations
                .Select(t => t.LangCode.ToLowerInvariant())
                .ToHashSet();

            if (!existingLangs.Contains("en"))
            {
                narration.Translations.Add(new TranslationEntity
                {
                    LangCode = "en",
                    TranslatedContent = $"{poi.Name} is a featured attraction in {poi.Province}."
                });
            }

            if (!existingLangs.Contains("fr"))
            {
                narration.Translations.Add(new TranslationEntity
                {
                    LangCode = "fr",
                    TranslatedContent = $"{poi.Name} est un site remarquable à {poi.Province}."
                });
            }

            if (PoiJson.ReadHighlights(poi.HighlightsJson).Count == 0)
            {
                poi.HighlightsJson = PoiJson.WriteHighlights([$"Điểm tham quan {poi.Category}"]);
            }

            if (PoiJson.ReadGallery(poi.GalleryJson).Count == 0)
            {
                var url = string.IsNullOrWhiteSpace(poi.ImageUrl) || poi.ImageUrl.Contains("placehold.co")
                    ? DefaultImageUrl
                    : poi.ImageUrl;
                poi.GalleryJson = PoiJson.WriteGallery(
                [
                    new PhotoJson(url, "https://soctrang.gov.vn", $"Hình ảnh {poi.Name}")
                ]);
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
