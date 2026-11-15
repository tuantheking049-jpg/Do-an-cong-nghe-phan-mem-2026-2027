namespace VisitorGuide.Infrastructure.Entities;

public sealed class AdminAccountEntity
{
    public int AdminID { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class LanguageEntity
{
    public string LangCode { get; set; } = "";
    public string LangName { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public sealed class PoiEntity
{
    public int PoiID { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Province { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "Hoạt động";
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; } = 5;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? ImageSourceUrl { get; set; }
    public string? ImagePosition { get; set; }

    public string? HighlightsJson { get; set; }

    public string? GalleryJson { get; set; }

    public NarrationEntity? Narration { get; set; }
}

public sealed class NarrationEntity
{
    public int NarrationID { get; set; }
    public int PoiID { get; set; }
    public string SourceLang { get; set; } = "vi";
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public string PublishStatus { get; set; } = "Bản nháp";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public PoiEntity Poi { get; set; } = null!;
    public List<TranslationEntity> Translations { get; set; } = [];
}

public sealed class TranslationEntity
{
    public int TranslationID { get; set; }
    public int NarrationID { get; set; }
    public string LangCode { get; set; } = "";
    public string TranslatedContent { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public NarrationEntity Narration { get; set; } = null!;
}

public sealed class AudioFileEntity
{
    public int AudioID { get; set; }
    public int NarrationID { get; set; }
    public string LangCode { get; set; } = "";
    public string? VoiceName { get; set; }
    public string SourceType { get; set; } = "TTS";
    public string AudioURL { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
