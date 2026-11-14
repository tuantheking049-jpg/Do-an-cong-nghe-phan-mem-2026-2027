using System.Collections.Concurrent;
using System.Text.Json;
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

public sealed class AdminPersistedState
{
    public Dictionary<string, int> OrderById { get; set; } = new();
    public Dictionary<string, string> StatusById { get; set; } = new();
    public Dictionary<string, string> NarrationTitleById { get; set; } = new();
    public Dictionary<string, string> NarrationPublishStatusById { get; set; } = new();
    public int NextOrder { get; set; } = 7;
}

[ApiController]
[Route("api/admin/landmarks")]
public class AdminDiaDiemController : ControllerBase
{
    private readonly VisitorGuideDbContext _dbContext;
    private const string EmptyNarrationPlaceholder = "(Chưa có nội dung thuyết minh)";
    private static readonly string StateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "admin_state.json");
    private static readonly object StateLock = new();

    // Lưu trạng thái địa điểm (UC12: Hoạt động / Bảo trì)
    private static readonly ConcurrentDictionary<string, string> _statusById = new(StringComparer.OrdinalIgnoreCase);

    // Lưu Tiêu đề & Trạng thái công bố thuyết minh (UC13: Đã công bố / Bản nháp)
    private static readonly ConcurrentDictionary<string, string> _narrationTitleById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> _narrationPublishStatusById = new(StringComparer.OrdinalIgnoreCase);

    // 6 địa điểm gốc luôn cố định từ 1 đến 6, địa điểm tạo mới luôn từ 7 trở đi (ở dưới cùng)
    private static readonly ConcurrentDictionary<string, int> _orderById = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cho-noi-nga-nam"] = 1,
        ["chua-doi"] = 2,
        ["chua-la-han"] = 3,
        ["chua-phat-hoc-2"] = 4,
        ["khu-can-cu-tinh-uy-soc-trang"] = 5,
        ["tan-hue-vien"] = 6
    };
    private static int _nextOrder = 7;

    static AdminDiaDiemController()
    {
        LoadStateFromDisk();
    }

    public AdminDiaDiemController(VisitorGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static void LoadStateFromDisk()
    {
        try
        {
            if (!System.IO.File.Exists(StateFilePath)) return;
            var json = System.IO.File.ReadAllText(StateFilePath);
            var state = JsonSerializer.Deserialize<AdminPersistedState>(json);
            if (state == null) return;

            foreach (var kv in state.OrderById) _orderById[kv.Key] = kv.Value;
            foreach (var kv in state.StatusById) _statusById[kv.Key] = kv.Value;
            foreach (var kv in state.NarrationTitleById) _narrationTitleById[kv.Key] = kv.Value;
            foreach (var kv in state.NarrationPublishStatusById) _narrationPublishStatusById[kv.Key] = kv.Value;
            if (state.NextOrder > _nextOrder) _nextOrder = state.NextOrder;

            // Đảm bảo 6 địa điểm gốc luôn giữ vị trí 1..6
            _orderById["cho-noi-nga-nam"] = 1;
            _orderById["chua-doi"] = 2;
            _orderById["chua-la-han"] = 3;
            _orderById["chua-phat-hoc-2"] = 4;
            _orderById["khu-can-cu-tinh-uy-soc-trang"] = 5;
            _orderById["tan-hue-vien"] = 6;
        }
        catch
        {
            // Bỏ qua nếu file chưa tồn tại hoặc lỗi đọc
        }
    }

    private static void SaveStateToDisk()
    {
        try
        {
            lock (StateLock)
            {
                var state = new AdminPersistedState
                {
                    OrderById = new Dictionary<string, int>(_orderById, StringComparer.OrdinalIgnoreCase),
                    StatusById = new Dictionary<string, string>(_statusById, StringComparer.OrdinalIgnoreCase),
                    NarrationTitleById = new Dictionary<string, string>(_narrationTitleById, StringComparer.OrdinalIgnoreCase),
                    NarrationPublishStatusById = new Dictionary<string, string>(_narrationPublishStatusById, StringComparer.OrdinalIgnoreCase),
                    NextOrder = _nextOrder
                };
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(StateFilePath, json);
            }
        }
        catch
        {
            // Không làm gián đoạn request nếu không ghi được file phụ
        }
    }

    public static int GetOrderIndex(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 9999;
        if (_orderById.TryGetValue(id.Trim(), out var order)) return order;

        lock (StateLock)
        {
            if (!_orderById.TryGetValue(id.Trim(), out order))
            {
                order = _nextOrder++;
                _orderById[id.Trim()] = order;
                SaveStateToDisk();
            }
            return order;
        }
    }

    public static bool IsMaintenance(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _statusById.TryGetValue(id.Trim(), out var status) &&
               (status.Equals("Bảo trì", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Đang bảo trì", StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // UC12: QUẢN LÝ ĐỊA ĐIỂM (CRUD)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> GetAllForAdmin()
    {
        await EnsureChildTablesPopulatedAsync();

        var rawList = await _dbContext.DiaDiem
            .AsNoTracking()
            .ToListAsync();

        foreach (var item in rawList)
        {
            GetOrderIndex(item.Id);
            if (!ThongKeController.LuotNgheTheoId.ContainsKey(item.Id))
            {
                ThongKeController.LuotNgheTheoId[item.Id] = 0;
            }
        }

        var danhSach = rawList
            .OrderBy(d => GetOrderIndex(d.Id))
            .Select(d => new
            {
                id = d.Id,
                name = d.Name,
                province = d.Province,
                category = d.Category,
                plays = ThongKeController.LuotNgheTheoId.TryGetValue(d.Id, out var p) ? p : 0,
                status = _statusById.TryGetValue(d.Id, out var st) ? st : "Hoạt động",
                orderIndex = GetOrderIndex(d.Id)
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
        var exists = await _dbContext.DiaDiem.AnyAsync(x => x.Id == id);
        if (exists)
        {
            return Conflict(new { message = $"Mã ID '{id}' đã tồn tại trong Database!" });
        }

        var name = request.Name.Trim();
        var category = string.IsNullOrWhiteSpace(request.Category) ? "Văn hóa" : request.Category.Trim();
        var province = string.IsNullOrWhiteSpace(request.Province) ? "Sóc Trăng" : request.Province.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Hoạt động" : request.Status.Trim();

        var entity = new DiaDiemEntity
        {
            Id = id,
            Name = name,
            Category = category,
            Province = province,
            DurationMinutes = 5,
            Rating = 0,
            ReviewCount = 0,
            ImageUrl = "https://images.vietnamtourism.gov.vn/vn/images/2014/Chonoinga5.jpg",
            ImageSourceUrl = "https://soctrang.gov.vn",
            ImagePosition = "center center",
            Descriptions = new List<DiaDiemMoTaEntity>
            {
                new() { DiaDiemId = id, LanguageCode = "vi", Description = EmptyNarrationPlaceholder },
                new() { DiaDiemId = id, LanguageCode = "en", Description = $"{name} is a featured {category} attraction located in {province}." },
                new() { DiaDiemId = id, LanguageCode = "fr", Description = $"{name} est un site remarquable ({category}) situé à {province}." }
            },
            Highlights = new List<DiaDiemDiemNoiBatEntity>
            {
                new() { DiaDiemId = id, Position = 1, Text = $"Điểm tham quan {category}" },
                new() { DiaDiemId = id, Position = 2, Text = $"Du lịch {province}" }
            },
            GalleryPhotos = new List<DiaDiemAnhEntity>
            {
                new()
                {
                    DiaDiemId = id,
                    Position = 1,
                    Url = "https://images.vietnamtourism.gov.vn/vn/images/2014/Chonoinga5.jpg",
                    SourceUrl = "https://soctrang.gov.vn",
                    Caption = $"Hình ảnh {name}"
                }
            }
        };

        _dbContext.DiaDiem.Add(entity);
        await _dbContext.SaveChangesAsync();

        _orderById[id] = _nextOrder++;
        _statusById[id] = status;
        _narrationPublishStatusById[id] = "Bản nháp";
        ThongKeController.LuotNgheTheoId[id] = request.Plays ?? 0;
        SaveStateToDisk();

        return Ok(new { message = "Thêm địa điểm mới vào Database thành công!", id = entity.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminDiaDiemRequest request)
    {
        var entity = await _dbContext.DiaDiem.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
        {
            return NotFound(new { message = "Không tìm thấy địa điểm cần sửa!" });
        }

        entity.Name = request.Name.Trim();
        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? entity.Category : request.Category.Trim();
        entity.Province = string.IsNullOrWhiteSpace(request.Province) ? entity.Province : request.Province.Trim();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            _statusById[id] = request.Status.Trim();
        }

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = "Cập nhật thông tin vào Database thành công!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _dbContext.DiaDiem.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
        {
            return NotFound(new { message = "Không tìm thấy địa điểm cần xóa!" });
        }

        _dbContext.DiaDiem.Remove(entity);
        await _dbContext.SaveChangesAsync();

        _statusById.TryRemove(id, out _);
        _orderById.TryRemove(id, out _);
        _narrationTitleById.TryRemove(id, out _);
        _narrationPublishStatusById.TryRemove(id, out _);
        ThongKeController.LuotNgheTheoId.TryRemove(id, out _);
        SaveStateToDisk();

        return Ok(new { message = "Đã xóa địa điểm khỏi Database thành công!" });
    }

    // =========================================================================
    // UC13: BIÊN SOẠN NỘI DUNG THUYẾT MINH GỐC
    // =========================================================================

    [HttpGet("narrations")]
    public async Task<IActionResult> GetAllNarrations()
    {
        await EnsureChildTablesPopulatedAsync();

        var rawList = await _dbContext.DiaDiem
            .AsNoTracking()
            .Include(d => d.Descriptions)
            .Include(d => d.Highlights)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var item in rawList)
        {
            GetOrderIndex(item.Id);
        }

        var result = rawList
            .OrderBy(d => GetOrderIndex(d.Id))
            .Select(d =>
            {
                var dbViDesc = d.Descriptions
                    .FirstOrDefault(x => x.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))?.Description ?? "";

                bool isEmpty = string.IsNullOrWhiteSpace(dbViDesc) || dbViDesc == EmptyNarrationPlaceholder;

                // Chỉ dùng 2 trạng thái ngắn gọn: "Bản nháp" và "Đã công bố"
                string publishStatus;
                if (_narrationPublishStatusById.TryGetValue(d.Id, out var savedStatus))
                {
                    publishStatus = savedStatus == "Đã công bố" ? "Đã công bố" : "Bản nháp";
                }
                else
                {
                    publishStatus = isEmpty ? "Bản nháp" : "Đã công bố";
                }

                string title = _narrationTitleById.TryGetValue(d.Id, out var savedTitle) && !string.IsNullOrWhiteSpace(savedTitle)
                    ? savedTitle
                    : (isEmpty ? $"Thuyết minh {d.Name}" : $"Thuyết minh {d.Name}");

                var highlights = d.Highlights
                    .OrderBy(h => h.Position)
                    .Select(h => h.Text)
                    .ToList();

                return new
                {
                    id = d.Id,
                    name = d.Name,
                    category = d.Category,
                    title = title,
                    durationMinutes = d.DurationMinutes,
                    imageUrl = d.ImageUrl,
                    descriptionVi = isEmpty ? "" : dbViDesc,
                    highlights = highlights,
                    highlightsText = string.Join(", ", highlights),
                    publishStatus = publishStatus,
                    hasContent = !isEmpty,
                    orderIndex = GetOrderIndex(d.Id)
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

        var place = await _dbContext.DiaDiem
            .Include(d => d.Descriptions)
            .Include(d => d.Highlights)
            .Include(d => d.GalleryPhotos)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (place == null)
        {
            return NotFound(new { message = "Địa điểm liên quan không tồn tại trong cơ sở dữ liệu (Luồng 6a)!" });
        }

        place.DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 5;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            place.ImageUrl = request.ImageUrl.Trim();
            var firstPhoto = place.GalleryPhotos.OrderBy(p => p.Position).FirstOrDefault();
            if (firstPhoto != null)
            {
                firstPhoto.Url = place.ImageUrl;
                firstPhoto.Caption = $"Hình ảnh {place.Name}";
            }
        }

        string cleanTitle = request.Title.Trim();
        string cleanContent = request.DescriptionVi.Trim();
        string pubStatus = (!string.IsNullOrWhiteSpace(request.PublishStatus) &&
                            request.PublishStatus.Trim().Equals("Bản nháp", StringComparison.OrdinalIgnoreCase))
            ? "Bản nháp"
            : "Đã công bố";

        _narrationTitleById[place.Id] = cleanTitle;
        _narrationPublishStatusById[place.Id] = pubStatus;

        var viEntity = place.Descriptions.FirstOrDefault(x => x.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase));
        if (viEntity != null)
        {
            viEntity.Description = cleanContent;
        }
        else
        {
            _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
            {
                DiaDiemId = place.Id,
                LanguageCode = "vi",
                Description = cleanContent
            });
        }

        if (!string.IsNullOrWhiteSpace(request.HighlightsText))
        {
            var items = request.HighlightsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (items.Count > 0)
            {
                _dbContext.DiemNoiBat.RemoveRange(place.Highlights);
                int pos = 1;
                foreach (var item in items)
                {
                    _dbContext.DiemNoiBat.Add(new DiaDiemDiemNoiBatEntity
                    {
                        DiaDiemId = place.Id,
                        Position = pos++,
                        Text = item.Length > 300 ? item[..300] : item
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = $"Đã lưu bài thuyết minh \"{cleanTitle}\" ở trạng thái [{pubStatus}] thành công!" });
    }

    [HttpDelete("{id}/narration")]
    public async Task<IActionResult> DeleteNarration(string id)
    {
        var viEntity = await _dbContext.MoTaDiaDiem
            .FirstOrDefaultAsync(x => x.DiaDiemId == id && x.LanguageCode == "vi");

        if (viEntity == null)
        {
            return NotFound(new { message = "Không tìm thấy bài thuyết minh cần xóa!" });
        }

        viEntity.Description = EmptyNarrationPlaceholder;
        _narrationTitleById.TryRemove(id, out _);
        _narrationPublishStatusById[id] = "Bản nháp";

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = "Đã xóa nội dung và chuyển về trạng thái Bản nháp!" });
    }

    private async Task EnsureChildTablesPopulatedAsync()
    {
        var places = await _dbContext.DiaDiem
            .Include(p => p.Descriptions)
            .Include(p => p.Highlights)
            .Include(p => p.GalleryPhotos)
            .AsSplitQuery()
            .ToListAsync();

        bool hasChanges = false;

        foreach (var place in places)
        {
            var existingLangs = place.Descriptions.Select(d => d.LanguageCode.ToLowerInvariant()).ToHashSet();
            if (!existingLangs.Contains("vi"))
            {
                _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
                {
                    DiaDiemId = place.Id,
                    LanguageCode = "vi",
                    Description = EmptyNarrationPlaceholder
                });
                hasChanges = true;
            }
            if (!existingLangs.Contains("en"))
            {
                _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
                {
                    DiaDiemId = place.Id,
                    LanguageCode = "en",
                    Description = $"{place.Name} is a featured attraction in {place.Province}."
                });
                hasChanges = true;
            }
            if (!existingLangs.Contains("fr"))
            {
                _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
                {
                    DiaDiemId = place.Id,
                    LanguageCode = "fr",
                    Description = $"{place.Name} est un site remarquable à {place.Province}."
                });
                hasChanges = true;
            }

            if (place.Highlights.Count == 0)
            {
                _dbContext.DiemNoiBat.Add(new DiaDiemDiemNoiBatEntity
                {
                    DiaDiemId = place.Id,
                    Position = 1,
                    Text = $"Điểm tham quan {place.Category}"
                });
                hasChanges = true;
            }

            if (place.GalleryPhotos.Count == 0)
            {
                _dbContext.AnhDiaDiem.Add(new DiaDiemAnhEntity
                {
                    DiaDiemId = place.Id,
                    Position = 1,
                    Url = string.IsNullOrWhiteSpace(place.ImageUrl) || place.ImageUrl.Contains("placehold.co")
                        ? "https://images.vietnamtourism.gov.vn/vn/images/2014/Chonoinga5.jpg"
                        : place.ImageUrl,
                    SourceUrl = "https://soctrang.gov.vn",
                    Caption = $"Hình ảnh {place.Name}"
                });
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}