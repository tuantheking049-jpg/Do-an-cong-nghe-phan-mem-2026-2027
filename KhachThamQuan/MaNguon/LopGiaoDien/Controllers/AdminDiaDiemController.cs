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

public sealed record AdminLanguageRequest(
    string Code,
    string Name,
    string VoiceLocale,
    string Status
);

public sealed class AdminLanguageItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string VoiceLocale { get; set; } = "";
    public string Status { get; set; } = "Đang hỗ trợ";
    public int OrderIndex { get; set; }
}

public sealed record AdminTranslationRequest(
    string DiaDiemId,
    string LanguageCode,
    string TranslatedContent,
    string Status,
    bool AllowOverwrite = false
);

public sealed record AdminVoiceRequest(
    string DiaDiemId,
    string LanguageCode,
    string SourceType,
    string VoiceName,
    double SpeechRate,
    double Pitch,
    string? AudioUrl,
    string Status
);

public sealed class AdminVoiceItem
{
    public string Key { get; set; } = "";
    public string DiaDiemId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string SourceType { get; set; } = "TTS"; // "TTS" hoặc "Upload"
    public string VoiceName { get; set; } = "";
    public double SpeechRate { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public string AudioUrl { get; set; } = "";
    public string Status { get; set; } = "Đã công bố";
    public int OrderIndex { get; set; }
}

public sealed class AdminPersistedState
{
    public Dictionary<string, int> OrderById { get; set; } = new();
    public Dictionary<string, string> StatusById { get; set; } = new();
    public Dictionary<string, string> NarrationTitleById { get; set; } = new();
    public Dictionary<string, string> NarrationPublishStatusById { get; set; } = new();
    public int NextOrder { get; set; } = 7;
    // Thêm 2 dòng này để lưu danh sách ngôn ngữ UC14 khi tắt web
    public List<AdminLanguageItem> Languages { get; set; } = new();
    public int NextLangOrder { get; set; } = 4;
    public Dictionary<string, string> TranslationStatusByKey { get; set; } = new();
}

[ApiController]
[Route("api/admin/landmarks")]
public class AdminDiaDiemController : ControllerBase
{
    private readonly VisitorGuideDbContext _dbContext;
    private const string EmptyNarrationPlaceholder = "(Chưa có nội dung thuyết minh)";

    private const string EmptyTranslationPlaceholder = "(Chưa có bản dịch)";
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
            if (state.Languages != null && state.Languages.Count > 0)
            {
                foreach (var lang in state.Languages)
                {
                    if (!string.IsNullOrWhiteSpace(lang.Code))
                        _languagesByCode[lang.Code.Trim().ToLowerInvariant()] = lang;
                }
            }
            if (state.NextLangOrder > _nextLangOrder) _nextLangOrder = state.NextLangOrder;
            if (state.TranslationStatusByKey != null)
            {
                foreach (var kv in state.TranslationStatusByKey)
                    _translationStatusByKey[kv.Key] = kv.Value;
            }
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
                    NextOrder = _nextOrder,
                    Languages = _languagesByCode.Values.OrderBy(x => x.OrderIndex).ToList(),
                    NextLangOrder = _nextLangOrder,
                    TranslationStatusByKey = new Dictionary<string, string>(_translationStatusByKey, StringComparer.OrdinalIgnoreCase)
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

    // Kiểm tra bài thuyết minh gốc của địa điểm có đang ở trạng thái "Bản nháp" hay không
    public static bool IsNarrationDraft(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        return _narrationPublishStatusById.TryGetValue(id.Trim(), out var st) &&
               st.Equals("Bản nháp", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    // Kiểm tra bản dịch cụ thể ở UC15 có đang ở trạng thái "Bản nháp" không
    public static bool IsTranslationDraft(string diaDiemId, string langCode)
    {
        if (string.IsNullOrWhiteSpace(diaDiemId) || string.IsNullOrWhiteSpace(langCode)) return false;
        if (IsNarrationDraft(diaDiemId)) return true;
        if (langCode.Trim().Equals("vi", StringComparison.OrdinalIgnoreCase)) return false;

        string key = GetTransKey(diaDiemId, langCode);
        return _translationStatusByKey.TryGetValue(key, out var st) &&
               st.Equals("Bản nháp", StringComparison.OrdinalIgnoreCase);
    }

    // Tự động dịch văn bản Tiếng Việt sang bất kỳ ngôn ngữ nào qua Google Translate
    public static async Task<string> TranslateViaGoogleAsync(string textVi, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(textVi)) return "";
        string tl = targetLang.Trim().ToLowerInvariant();
        if (tl == "vi") return textVi;
        if (tl == "jp") tl = "ja"; // Chuẩn mã ISO của tiếng Nhật trên Google Translate là 'ja'

        try
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={Uri.EscapeDataString(tl)}&dt=t&q={Uri.EscapeDataString(textVi)}";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var sb = new System.Text.StringBuilder();
            foreach (var segment in doc.RootElement[0].EnumerateArray())
            {
                if (segment[0].ValueKind == JsonValueKind.String)
                    sb.Append(segment[0].GetString());
            }
            var translated = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(translated) ? textVi : translated;
        }
        catch
        {
            return textVi;
        }
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
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Bảo Trì" : request.Status.Trim();

        const string defaultValidImage = "https://images.vietnamtourism.gov.vn/vn/images/2014/AnhInternet/chonoinga5.jpg";

        // 1. Tạo danh sách mô tả và tự động dịch sang mọi ngôn ngữ đang có (Đặt TRƯỚC var entity)
        string defaultViText = $"{name} là điểm tham quan tiêu biểu thuộc phân loại {category} tại {province}, mang đậm nét đặc trưng văn hóa và du lịch địa phương.";
        var descriptionsList = new List<DiaDiemMoTaEntity>
        {
            new() { DiaDiemId = id, LanguageCode = "vi", Description = defaultViText }
        };

        foreach (var lang in _languagesByCode.Values.Where(l => !l.Code.Equals("vi", StringComparison.OrdinalIgnoreCase)))
        {
            string translated = await TranslateViaGoogleAsync(defaultViText, lang.Code);
            descriptionsList.Add(new DiaDiemMoTaEntity
            {
                DiaDiemId = id,
                LanguageCode = lang.Code.ToLowerInvariant(),
                Description = translated
            });
            _translationStatusByKey[GetTransKey(id, lang.Code)] = "Bản nháp";
        }

        // 2. Gán descriptionsList vào trong DiaDiemEntity
        var entity = new DiaDiemEntity
        {
            Id = id,
            Name = name,
            Category = category,
            Province = province,
            DurationMinutes = 5,
            Rating = 0,
            ReviewCount = 0,
            ImageUrl = defaultValidImage,
            ImageSourceUrl = "https://soctrang.gov.vn",
            ImagePosition = "center center",
            Descriptions = descriptionsList,
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
                    Url = defaultValidImage,
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
        _narrationTitleById[place.Id] = cleanTitle;
        _narrationPublishStatusById[place.Id] = pubStatus;

        // Đồng bộ trạng thái công bố từ UC13 sang toàn bộ các bản dịch của địa điểm này ở UC15
        foreach (var lang in _languagesByCode.Values.Where(l => !l.Code.Equals("vi", StringComparison.OrdinalIgnoreCase)))
        {
            _translationStatusByKey[GetTransKey(place.Id, lang.Code)] = pubStatus;
        }

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
        foreach (var lang in _languagesByCode.Values.Where(l => !l.Code.Equals("vi", StringComparison.OrdinalIgnoreCase)))
        {
            _translationStatusByKey[GetTransKey(id, lang.Code)] = "Bản nháp";
        }

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = "Đã xóa nội dung và chuyển về trạng thái Bản nháp!" });
    }

// =========================================================================
    // UC14: CẤU HÌNH NGÔN NGỮ HỖ TRỢ
    // =========================================================================
    private static readonly ConcurrentDictionary<string, AdminLanguageItem> _languagesByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = new AdminLanguageItem { Code = "vi", Name = "Tiếng Việt", VoiceLocale = "vi-VN", Status = "Đang hỗ trợ", OrderIndex = 1 },
        ["en"] = new AdminLanguageItem { Code = "en", Name = "Tiếng Anh (English)", VoiceLocale = "en-US", Status = "Đang hỗ trợ", OrderIndex = 2 },
        ["fr"] = new AdminLanguageItem { Code = "fr", Name = "Tiếng Pháp (Français)", VoiceLocale = "fr-FR", Status = "Đang hỗ trợ", OrderIndex = 3 }
    };
    private static int _nextLangOrder = 4;

    // Kiểm tra ngôn ngữ có tồn tại và đang ở trạng thái "Đang hỗ trợ" hay không
    public static bool IsLanguageSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return _languagesByCode.TryGetValue(code.Trim().ToLowerInvariant(), out var lang) &&
               lang.Status.Equals("Đang hỗ trợ", StringComparison.OrdinalIgnoreCase);
    }

    public static AdminLanguageItem? GetLanguageInfo(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return _languagesByCode.TryGetValue(code.Trim().ToLowerInvariant(), out var lang) ? lang : null;
    }

    // 1. GET: Lấy danh sách ngôn ngữ kèm số bản dịch thực tế trong bảng MoTaDiaDiem
    // Tự động bổ sung tên bản ngữ vào trong ngoặc đơn cho mọi ngôn ngữ: VD "Tiếng Nhật" -> "Tiếng Nhật (日本語)"
    private static string FormatLanguageNativeName(string code, string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return rawName;
        string cleanName = rawName.Trim();
        string normCode = (code ?? "").Trim().ToLowerInvariant();

        if (normCode == "vi" || cleanName.Contains('(')) return cleanName;

        string isoCode = normCode switch
        {
            "jp" => "ja",
            "kr" => "ko",
            "cn" => "zh",
            _ => normCode.Split('-')[0]
        };

        try
        {
            var culture = new System.Globalization.CultureInfo(isoCode);
            string native = culture.NativeName.Split('(')[0].Trim();
            if (!string.IsNullOrWhiteSpace(native) && ! native.Equals(isoCode, StringComparison.OrdinalIgnoreCase))
            {
                native = char.ToUpper(native[0]) + native[1..];
                return $"{cleanName} ({native})";
            }
        }
        catch { }

        return cleanName;
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetAllLanguages()
    {
        var usageCounts = await _dbContext.MoTaDiaDiem
            .AsNoTracking()
            .Where(m => m.Description != "" &&
                        m.Description != EmptyNarrationPlaceholder &&
                        m.Description != EmptyTranslationPlaceholder)
            .GroupBy(m => m.LanguageCode.ToLower())
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count, StringComparer.OrdinalIgnoreCase);

        bool updatedNames = false;
        foreach (var lang in _languagesByCode.Values)
        {
            string formatted = FormatLanguageNativeName(lang.Code, lang.Name);
            if (lang.Name != formatted)
            {
                lang.Name = formatted;
                updatedNames = true;
            }
        }
        if (updatedNames) SaveStateToDisk();

        var result = _languagesByCode.Values
            .OrderBy(l => l.OrderIndex)
            .Select(l => new
            {
                code = l.Code,
                name = l.Name,
                voiceLocale = l.VoiceLocale,
                status = l.Status,
                usedCount = usageCounts.TryGetValue(l.Code, out var c) ? c : 0,
                orderIndex = l.OrderIndex
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("languages")]
    public async Task<IActionResult> CreateLanguage([FromBody] AdminLanguageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Mã ngôn ngữ và Tên ngôn ngữ không được để trống (Luồng 5a)!" });
        }

        string code = request.Code.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-z]{2,5}(-[a-z]{2,5})?$"))
        {
            return BadRequest(new { message = $"Mã ngôn ngữ '{code}' không hợp lệ! Chỉ dùng chữ cái viết thường, VD: zh, ja, ko (Luồng 5a)." });
        }

        if (_languagesByCode.ContainsKey(code))
        {
            return Conflict(new { message = $"Mã ngôn ngữ '{code}' đã tồn tại trong hệ thống! Vui lòng điều chỉnh (Luồng 5a)." });
        }

        string defaultLocale = code switch
        {
            "ja" or "jp" => "ja-JP",
            "ko" or "kr" => "ko-KR",
            "zh" or "cn" => "zh-CN",
            _ => $"{code}-{code.ToUpperInvariant()}"
        };

        var item = new AdminLanguageItem
        {
            Code = code,
            Name = FormatLanguageNativeName(code, request.Name),
            VoiceLocale = string.IsNullOrWhiteSpace(request.VoiceLocale) ? defaultLocale : request.VoiceLocale.Trim(),
            Status = (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim() == "Tạm ngưng") ? "Tạm ngưng" : "Đang hỗ trợ",
            OrderIndex = _nextLangOrder++
        };

        _languagesByCode[code] = item;

        var places = await _dbContext.DiaDiem.Include(p => p.Descriptions).ToListAsync();
        foreach (var place in places)
        {
            var viText = place.Descriptions.FirstOrDefault(d => d.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))?.Description
                         ?? $"{place.Name} là điểm tham quan tiêu biểu thuộc phân loại {place.Category} tại {place.Province}.";
            string translated = await TranslateViaGoogleAsync(viText, code);

            var existingRow = place.Descriptions.FirstOrDefault(d => d.LanguageCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (existingRow != null)
            {
                existingRow.Description = translated;
            }
            else
            {
                _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
                {
                    DiaDiemId = place.Id,
                    LanguageCode = code,
                    Description = translated
                });
            }

            string key = GetTransKey(place.Id, code);
            _translationStatusByKey[key] = IsNarrationDraft(place.Id) ? "Bản nháp" : "Đã công bố";
            GetTranslationOrder(place.Id, code);
        }

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = $"Đã thêm ngôn ngữ \"{item.Name}\" ({item.Code}) và tự động dịch nội dung thuyết minh!" });
    }

    [HttpPut("languages/{code}")]
    public IActionResult UpdateLanguage(string code, [FromBody] AdminLanguageRequest request)
    {
        string key = code.Trim().ToLowerInvariant();
        if (!_languagesByCode.TryGetValue(key, out var existing))
        {
            return NotFound(new { message = "Không tìm thấy ngôn ngữ cần chỉnh sửa!" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên ngôn ngữ không được để trống (Luồng 5a)!" });
        }

        existing.Name = FormatLanguageNativeName(key, request.Name);
        if (!string.IsNullOrWhiteSpace(request.VoiceLocale))
        {
            existing.VoiceLocale = request.VoiceLocale.Trim();
        }
        existing.Status = (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim() == "Tạm ngưng")
            ? "Tạm ngưng"
            : "Đang hỗ trợ";

        SaveStateToDisk();
        return Ok(new { message = $"Đã cập nhật cấu hình ngôn ngữ \"{existing.Name}\" thành công!" });
    }

    [HttpDelete("languages/{code}")]
    public async Task<IActionResult> DeleteLanguage(string code)
    {
        string key = code.Trim().ToLowerInvariant();
        if (!_languagesByCode.TryGetValue(key, out var existing))
        {
            return NotFound(new { message = "Không tìm thấy ngôn ngữ cần xóa!" });
        }

        if (key == "vi")
        {
            return BadRequest(new { message = "Không thể xóa Tiếng Việt (vi) vì đây là ngôn ngữ thuyết minh gốc mặc định của hệ thống (Luồng 6a)!" });
        }

        int usedCount = await _dbContext.MoTaDiaDiem
            .CountAsync(m => m.LanguageCode.ToLower() == key &&
                             m.Description != "" &&
                             m.Description != EmptyNarrationPlaceholder &&
                             m.Description != EmptyTranslationPlaceholder);

        if (usedCount > 0)
        {
            return BadRequest(new
            {
                message = $"Từ chối xóa (Luồng 6a): Ngôn ngữ \"{existing.Name}\" ({key}) đang được {usedCount} bản dịch sử dụng! Hãy xóa các bản dịch của ngôn ngữ này ở mục Quản lý bản dịch trước hoặc chuyển trạng thái sang 'Tạm ngưng'."
            });
        }

        var dummyRows = await _dbContext.MoTaDiaDiem.Where(m => m.LanguageCode.ToLower() == key).ToListAsync();
        if (dummyRows.Count > 0)
        {
            _dbContext.MoTaDiaDiem.RemoveRange(dummyRows);
            await _dbContext.SaveChangesAsync();
        }

        _languagesByCode.TryRemove(key, out _);
        SaveStateToDisk();

        return Ok(new { message = $"Đã xóa hoàn toàn ngôn ngữ \"{existing.Name}\" ({key}) khỏi hệ thống!" });
    }

    // =========================================================================
    // UC15: QUẢN LÝ BẢN DỊCH (BIÊN DỊCH NỘI DUNG)
    // =========================================================================
    private static readonly ConcurrentDictionary<string, string> _translationStatusByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, int> _translationOrderByKey = new(StringComparer.OrdinalIgnoreCase);
    private static int _nextTranslationOrder = 1;

    private static string GetTransKey(string diaDiemId, string langCode) =>
        $"{diaDiemId.Trim().ToLowerInvariant()}:{langCode.Trim().ToLowerInvariant()}";

    private static int GetTranslationOrder(string diaDiemId, string langCode)
    {
        string key = GetTransKey(diaDiemId, langCode);
        if (_translationOrderByKey.TryGetValue(key, out var order)) return order;

        lock (StateLock)
        {
            if (!_translationOrderByKey.TryGetValue(key, out order))
            {
                order = _nextTranslationOrder++;
                _translationOrderByKey[key] = order;
            }
            return order;
        }
    }

    // 1. GET: Lấy danh sách các bản dịch hiện có (ngoại trừ tiếng Việt gốc)
    [HttpGet("translations")]
    public async Task<IActionResult> GetAllTranslations()
    {
        await EnsureChildTablesPopulatedAsync();

        var rawPlaces = await _dbContext.DiaDiem
            .AsNoTracking()
            .Include(d => d.Descriptions)
            .AsSplitQuery()
            .ToListAsync();

        var orderedPlaces = rawPlaces.OrderBy(d => GetOrderIndex(d.Id)).ToList();

        // Khởi tạo thứ tự mặc định cho các bản dịch gốc ban đầu
        foreach (var p in orderedPlaces)
        {
            foreach (var desc in p.Descriptions.Where(x => !x.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.LanguageCode))
            {
                if (!string.IsNullOrWhiteSpace(desc.Description) && desc.Description != EmptyTranslationPlaceholder)
                {
                    GetTranslationOrder(p.Id, desc.LanguageCode);
                }
            }
        }

        var items = new List<object>();

        foreach (var p in orderedPlaces)
        {
            var viOriginal = p.Descriptions
                .FirstOrDefault(x => x.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))?.Description ?? "";
            bool hasViOriginal = !string.IsNullOrWhiteSpace(viOriginal) && viOriginal != EmptyNarrationPlaceholder;

            foreach (var desc in p.Descriptions.Where(x => !x.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrWhiteSpace(desc.Description) || desc.Description == EmptyTranslationPlaceholder)
                {
                    continue; // Đã xóa hoặc chưa dịch thì không đưa vào danh sách hiện có
                }

                string langCode = desc.LanguageCode.ToLowerInvariant();
                string langName = _languagesByCode.TryGetValue(langCode, out var langObj)
                    ? langObj.Name
                    : langCode.ToUpperInvariant();

                string key = GetTransKey(p.Id, langCode);
                string defaultStatus = IsNarrationDraft(p.Id) ? "Bản nháp" : "Đã công bố";
                string status = _translationStatusByKey.TryGetValue(key, out var st) ? st : defaultStatus;
                int orderIdx = GetTranslationOrder(p.Id, langCode);

                items.Add(new
                {
                    key = key,
                    diaDiemId = p.Id,
                    diaDiemName = p.Name,
                    category = p.Category,
                    languageCode = langCode,
                    languageName = langName,
                    originalContentVi = hasViOriginal ? viOriginal : "(Chưa có bài thuyết minh gốc Tiếng Việt)",
                    translatedContent = desc.Description,
                    status = status,
                    orderIndex = orderIdx
                });
            }
        }

        var sorted = items
            .OrderBy(x => (int)x.GetType().GetProperty("orderIndex")!.GetValue(x)!)
            .ToList();

        return Ok(sorted);
    }

    // 2. POST / PUT: Tạo mới hoặc cập nhật bản dịch
    [HttpPost("translations")]
    public async Task<IActionResult> SaveTranslation([FromBody] AdminTranslationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DiaDiemId) || string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            return BadRequest(new { message = "Vui lòng chọn Bài thuyết minh gốc và Ngôn ngữ đích (Luồng 3a)!" });
        }

        // Luồng phụ 6a: Kiểm tra nội dung bản dịch bị thiếu
        if (string.IsNullOrWhiteSpace(request.TranslatedContent))
        {
            return BadRequest(new { message = "Nội dung bản dịch không được để trống! Vui lòng bổ sung (Luồng 6a)." });
        }

        string placeId = request.DiaDiemId.Trim();
        string langCode = request.LanguageCode.Trim().ToLowerInvariant();

        if (langCode == "vi")
        {
            return BadRequest(new { message = "Tiếng Việt (vi) là ngôn ngữ gốc, vui lòng chọn ngôn ngữ đích khác!" });
        }

        // Luồng phụ 3a: Kiểm tra ngôn ngữ đích và bài thuyết minh gốc có tồn tại không
        if (!_languagesByCode.TryGetValue(langCode, out var langInfo))
        {
            return NotFound(new { message = $"Ngôn ngữ đích '{langCode}' chưa được khai báo trong hệ thống (Luồng 3a)!" });
        }

        var place = await _dbContext.DiaDiem
            .Include(d => d.Descriptions)
            .FirstOrDefaultAsync(d => d.Id == placeId);

        if (place == null)
        {
            return NotFound(new { message = "Địa điểm của bài thuyết minh gốc không tồn tại (Luồng 3a)!" });
        }

        var existingTrans = place.Descriptions
            .FirstOrDefault(x => x.LanguageCode.Equals(langCode, StringComparison.OrdinalIgnoreCase));

        bool alreadyHasContent = existingTrans != null &&
                                 !string.IsNullOrWhiteSpace(existingTrans.Description) &&
                                 existingTrans.Description != EmptyTranslationPlaceholder;

        // Luồng phụ 7a: Bản dịch cho cùng bài và ngôn ngữ đã tồn tại khi tạo mới
        if (alreadyHasContent && !request.AllowOverwrite)
        {
            return Conflict(new
            {
                code = "TRANSLATION_EXISTS",
                message = $"Bản dịch ngôn ngữ \"{langInfo.Name}\" cho địa điểm \"{place.Name}\" đã tồn tại (Luồng 7a)!"
            });
        }

        if (existingTrans != null)
        {
            existingTrans.Description = request.TranslatedContent.Trim();
        }
        else
        {
            _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
            {
                DiaDiemId = place.Id,
                LanguageCode = langCode,
                Description = request.TranslatedContent.Trim()
            });
        }

        string key = GetTransKey(place.Id, langCode);
        _translationStatusByKey[key] = (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim() == "Bản nháp")
            ? "Bản nháp"
            : "Đã công bố";

        // Nếu là bản dịch tạo mới hoàn toàn thì xếp xuống cuối danh sách
        if (!alreadyHasContent)
        {
            _translationOrderByKey[key] = _nextTranslationOrder++;
        }
        else
        {
            GetTranslationOrder(place.Id, langCode);
        }

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new
        {
            message = $"Đã lưu bản dịch [{langInfo.Name}] cho địa điểm \"{place.Name}\" thành công!"
        });
    }

    // 3. DELETE: Xóa bản dịch
    [HttpDelete("translations/{diaDiemId}/{langCode}")]
    public async Task<IActionResult> DeleteTranslation(string diaDiemId, string langCode)
    {
        string placeId = diaDiemId.Trim();
        string code = langCode.Trim().ToLowerInvariant();

        var transEntity = await _dbContext.MoTaDiaDiem
            .FirstOrDefaultAsync(x => x.DiaDiemId == placeId && x.LanguageCode.ToLower() == code);

        if (transEntity == null)
        {
            return NotFound(new { message = "Không tìm thấy bản dịch cần xóa!" });
        }

        // Đặt chuỗi đánh dấu đã xóa để hệ thống không tự động tạo lại khi load trang
        transEntity.Description = EmptyTranslationPlaceholder;

        string key = GetTransKey(placeId, code);
        _translationStatusByKey.TryRemove(key, out _);
        _translationOrderByKey.TryRemove(key, out _);
        _voicesByKey.TryRemove(key, out _);

        await _dbContext.SaveChangesAsync();
        SaveStateToDisk();

        return Ok(new { message = $"Đã xóa bản dịch ({code}) của địa điểm \"{placeId}\" thành công!" });
    }

    // =========================================================================
    // UC16: QUẢN LÝ GIỌNG ĐỌC THUYẾT MINH (TTS & AUDIO FILE)
    // =========================================================================
    private static readonly ConcurrentDictionary<string, AdminVoiceItem> _voicesByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, bool> _deletedVoiceKeys = new(StringComparer.OrdinalIgnoreCase);
    private static int _nextVoiceOrder = 1;

    private static string GetDefaultVoiceName(string langCode) => langCode.ToLowerInvariant() switch
    {
        "vi" => "Google Tiếng Việt (vi-VN)",
        "en" => "Google US English (en-US)",
        "fr" => "Google Français (fr-FR)",
        "ja" or "jp" => "Google 日本語 (ja-JP)",
        "ko" => "Google 한국어 (ko-KR)",
        "zh" => "Google 普通话 (zh-CN)",
        _ => $"Google TTS ({langCode})"
    };

    // 1. GET: Lấy danh sách âm thanh thuyết minh (Tự động sinh cho TẤT CẢ ngôn ngữ có văn bản)
    [HttpGet("voices")]
    public async Task<IActionResult> GetAllVoices()
    {
        await EnsureChildTablesPopulatedAsync();

        var rawPlaces = await _dbContext.DiaDiem
            .AsNoTracking()
            .Include(d => d.Descriptions)
            .AsSplitQuery()
            .ToListAsync();

        var orderedPlaces = rawPlaces.OrderBy(d => GetOrderIndex(d.Id)).ToList();
        var orderedLangCodes = _languagesByCode.Values
            .OrderBy(l => l.OrderIndex)
            .Select(l => l.Code.ToLowerInvariant())
            .ToList();

        // Tự động khởi tạo cấu hình TTS mặc định cho TẤT CẢ ngôn ngữ có nội dung chữ
        foreach (var p in orderedPlaces)
        {
            var sortedDescs = p.Descriptions
                .OrderBy(d =>
                {
                    int idx = orderedLangCodes.IndexOf(d.LanguageCode.ToLowerInvariant());
                    return idx >= 0 ? idx : 999;
                })
                .ToList();

            foreach (var desc in sortedDescs)
            {
                if (string.IsNullOrWhiteSpace(desc.Description) ||
                    desc.Description == EmptyNarrationPlaceholder ||
                    desc.Description == EmptyTranslationPlaceholder)
                {
                    continue;
                }

                string langCode = desc.LanguageCode.ToLowerInvariant();
                string key = GetTransKey(p.Id, langCode);

                if (!_voicesByKey.ContainsKey(key) && !_deletedVoiceKeys.ContainsKey(key))
                {
                    _voicesByKey[key] = new AdminVoiceItem
                    {
                        Key = key,
                        DiaDiemId = p.Id,
                        LanguageCode = langCode,
                        SourceType = "TTS",
                        VoiceName = GetDefaultVoiceName(langCode),
                        SpeechRate = 1.0,
                        Pitch = 1.0,
                        AudioUrl = "",
                        Status = "Đã công bố",
                        OrderIndex = _nextVoiceOrder++
                    };
                }
            }
        }

        var placeMap = orderedPlaces.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
        var result = new List<object>();

        foreach (var v in _voicesByKey.Values.OrderBy(x => x.OrderIndex))
        {
            if (!placeMap.TryGetValue(v.DiaDiemId, out var place)) continue;

            var descObj = place.Descriptions
                .FirstOrDefault(x => x.LanguageCode.Equals(v.LanguageCode, StringComparison.OrdinalIgnoreCase));

            string textContent = descObj?.Description ?? "";
            if (textContent == EmptyNarrationPlaceholder || textContent == EmptyTranslationPlaceholder)
            {
                continue; // Nếu văn bản đã bị xóa thì ẩn luôn khỏi danh sách giọng đọc
            }

            string langName = _languagesByCode.TryGetValue(v.LanguageCode, out var langInfo)
                ? langInfo.Name
                : v.LanguageCode.ToUpperInvariant();

            string voiceLocale = _languagesByCode.TryGetValue(v.LanguageCode, out var lObj) && !string.IsNullOrWhiteSpace(lObj.VoiceLocale)
                ? lObj.VoiceLocale
                : v.LanguageCode;

            result.Add(new
            {
                key = v.Key,
                diaDiemId = v.DiaDiemId,
                diaDiemName = place.Name,
                category = place.Category,
                languageCode = v.LanguageCode,
                languageName = langName,
                voiceLocale = voiceLocale,
                sourceType = v.SourceType,
                voiceName = v.VoiceName,
                speechRate = v.SpeechRate,
                pitch = v.Pitch,
                audioUrl = v.AudioUrl,
                textContent = textContent,
                status = v.Status,
                orderIndex = v.OrderIndex
            });
        }

        return Ok(result);
    }

    // 2. POST: Tạo mới hoặc cập nhật giọng đọc / tệp âm thanh thuyết minh
    [HttpPost("voices")]
    public async Task<IActionResult> SaveVoice([FromBody] AdminVoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DiaDiemId) || string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            return BadRequest(new { message = "Vui lòng chọn Địa điểm và Ngôn ngữ thuyết minh!" });
        }

        string placeId = request.DiaDiemId.Trim();
        string langCode = request.LanguageCode.Trim().ToLowerInvariant();

        var place = await _dbContext.DiaDiem
            .AsNoTracking()
            .Include(d => d.Descriptions)
            .FirstOrDefaultAsync(d => d.Id == placeId);

        if (place == null)
        {
            return NotFound(new { message = "Địa điểm đã chọn không tồn tại trong Database!" });
        }

        // Luồng phụ 4a: Kiểm tra xem đã có nội dung thuyết minh ở ngôn ngữ đã chọn chưa
        var descEntity = place.Descriptions
            .FirstOrDefault(x => x.LanguageCode.Equals(langCode, StringComparison.OrdinalIgnoreCase));

        if (descEntity == null ||
            string.IsNullOrWhiteSpace(descEntity.Description) ||
            descEntity.Description == EmptyNarrationPlaceholder ||
            descEntity.Description == EmptyTranslationPlaceholder)
        {
            return BadRequest(new
            {
                message = $"Chưa có nội dung thuyết minh ở ngôn ngữ '{langCode}' cho địa điểm \"{place.Name}\"! Vui lòng bổ sung nội dung trước khi tạo âm thanh (Luồng 4a)."
            });
        }

        string sourceType = (!string.IsNullOrWhiteSpace(request.SourceType) &&
                             request.SourceType.Trim().Equals("Upload", StringComparison.OrdinalIgnoreCase))
            ? "Upload"
            : "TTS";

        string audioUrl = request.AudioUrl?.Trim() ?? "";

        // Luồng phụ 7a: Kiểm tra tệp âm thanh hợp lệ nếu chọn phương thức tải tệp
        if (sourceType == "Upload")
        {
            bool isValidAudio = !string.IsNullOrWhiteSpace(audioUrl) &&
                                (audioUrl.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                 audioUrl.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                 audioUrl.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                                 audioUrl.StartsWith("data:audio/", StringComparison.OrdinalIgnoreCase));

            if (!isValidAudio)
            {
                return BadRequest(new
                {
                    message = "Tệp âm thanh không hợp lệ (Chỉ chấp nhận định dạng .mp3, .wav, .ogg)! Hệ thống từ chối lưu (Luồng 7a)."
                });
            }
        }

        double rate = request.SpeechRate is >= 0.5 and <= 2.0 ? Math.Round(request.SpeechRate, 1) : 1.0;
        double pitch = request.Pitch is >= 0.5 and <= 2.0 ? Math.Round(request.Pitch, 1) : 1.0;
        string status = (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim() == "Bản nháp")
            ? "Bản nháp"
            : "Đã công bố";

        string key = GetTransKey(placeId, langCode);
        _deletedVoiceKeys.TryRemove(key, out _);

        int orderIdx = _voicesByKey.TryGetValue(key, out var existing)
            ? existing.OrderIndex
            : _nextVoiceOrder++;

        _voicesByKey[key] = new AdminVoiceItem
        {
            Key = key,
            DiaDiemId = placeId,
            LanguageCode = langCode,
            SourceType = sourceType,
            VoiceName = string.IsNullOrWhiteSpace(request.VoiceName) ? GetDefaultVoiceName(langCode) : request.VoiceName.Trim(),
            SpeechRate = rate,
            Pitch = pitch,
            AudioUrl = audioUrl,
            Status = status,
            OrderIndex = orderIdx
        };

        return Ok(new
        {
            message = $"Đã lưu cấu hình âm thanh thuyết minh ({langCode.ToUpperInvariant()}) cho \"{place.Name}\" thành công!"
        });
    }

    // 3. DELETE: Xóa âm thanh thuyết minh
    [HttpDelete("voices/{diaDiemId}/{langCode}")]
    public IActionResult DeleteVoice(string diaDiemId, string langCode)
    {
        string key = GetTransKey(diaDiemId, langCode);
        if (!_voicesByKey.TryRemove(key, out _))
        {
            return NotFound(new { message = "Không tìm thấy cấu hình âm thanh cần xóa!" });
        }

        _deletedVoiceKeys[key] = true;
        return Ok(new { message = "Đã xóa âm thanh thuyết minh thành công!" });
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
            // 1. Khôi phục nội dung gốc chuẩn cho Chùa La Hán nếu đang bị dính dòng chữ chờ công bố
            var viRow = place.Descriptions.FirstOrDefault(d => d.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase));
            if (viRow != null && (viRow.Description.Contains("đang chờ công bố") || viRow.Description == EmptyNarrationPlaceholder))
            {
                viRow.Description = place.Id.Equals("chua-la-han", StringComparison.OrdinalIgnoreCase)
                    ? "Chùa La Hán là ngôi chùa gắn với cộng đồng người Hoa ở Sóc Trăng. Ghé thăm để khám phá những chi tiết kiến trúc nhiều màu sắc, không gian thờ tự trang nghiêm và cảnh quan thanh tịnh."
                    : $"{place.Name} là điểm tham quan tiêu biểu thuộc nhóm {place.Category} tại {place.Province}.";
                _narrationPublishStatusById[place.Id] = "Đã công bố";
                hasChanges = true;
            }

            var viBaseText = place.Descriptions.FirstOrDefault(d => d.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))?.Description
                             ?? $"{place.Name} là điểm tham quan tiêu biểu thuộc nhóm {place.Category} tại {place.Province}.";

            // 2. Tự động dịch nội dung sang các ngôn ngữ đã khai báo trong UC14 (bỏ qua nếu đã xóa)
            foreach (var lang in _languagesByCode.Values.OrderBy(l => l.OrderIndex))
            {
                string code = lang.Code.ToLowerInvariant();
                var existingDesc = place.Descriptions.FirstOrDefault(d => d.LanguageCode.Equals(code, StringComparison.OrdinalIgnoreCase));

                if (existingDesc == null)
                {
                    string translated = code == "vi" ? viBaseText : await TranslateViaGoogleAsync(viBaseText, code);
                    _dbContext.MoTaDiaDiem.Add(new DiaDiemMoTaEntity
                    {
                        DiaDiemId = place.Id,
                        LanguageCode = code,
                        Description = translated
                    });
                    hasChanges = true;
                }
                else if (code != "vi" &&
                         existingDesc.Description != EmptyTranslationPlaceholder &&
                         (existingDesc.Description.StartsWith("[") || existingDesc.Description.Contains("is a featured Văn hóa")))
                {
                    existingDesc.Description = await TranslateViaGoogleAsync(viBaseText, code);
                    hasChanges = true;
                }
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
                        ? "https://upload.wikimedia.org/wikipedia/commons/thumb/5/50/Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng_01.jpg/1280px-Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng_01.jpg"
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