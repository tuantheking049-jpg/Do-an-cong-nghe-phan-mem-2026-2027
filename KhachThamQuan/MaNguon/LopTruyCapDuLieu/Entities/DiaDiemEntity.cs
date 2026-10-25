namespace VisitorGuide.Infrastructure.Entities;

public sealed class DiaDiemEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Province { get; set; } = "";
    public int DurationMinutes { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string ImageUrl { get; set; } = "";
    public string ImageSourceUrl { get; set; } = "";
    public string ImagePosition { get; set; } = "";
    public List<DiaDiemMoTaEntity> Descriptions { get; set; } = [];
    public List<DiaDiemDiemNoiBatEntity> Highlights { get; set; } = [];
    public List<DiaDiemAnhEntity> GalleryPhotos { get; set; } = [];
}

public sealed class DiaDiemMoTaEntity
{
    public string DiaDiemId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string Description { get; set; } = "";
    public DiaDiemEntity DiaDiem { get; set; } = null!;
}

public sealed class DiaDiemDiemNoiBatEntity
{
    public int Id { get; set; }
    public string DiaDiemId { get; set; } = "";
    public int Position { get; set; }
    public string Text { get; set; } = "";
    public DiaDiemEntity DiaDiem { get; set; } = null!;
}

public sealed class DiaDiemAnhEntity
{
    public int Id { get; set; }
    public string DiaDiemId { get; set; } = "";
    public int Position { get; set; }
    public string Url { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string Caption { get; set; } = "";
    public DiaDiemEntity DiaDiem { get; set; } = null!;
}
