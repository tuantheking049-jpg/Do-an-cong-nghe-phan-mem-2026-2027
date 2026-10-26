using VisitorGuide.Application;
using VisitorGuide.Application.Services;
using VisitorGuide.Infrastructure.Repositories;

namespace VisitorGuide.Application.Tests;

public sealed class VisitorGuideServiceTests
{
    private readonly VisitorGuideService _service = new(new KhoDiaDiemThamQuanTrongBoNho());

    [Fact]
    public void GetDanhSachDiaDiem_ReturnsOnlyTheSixSocTrangDestinations()
    {
        var result = _service.GetDanhSachDiaDiem(null, null, "vi");

        Assert.Equal(6, result.Count);
        Assert.All(result, landmark => Assert.Equal("Sóc Trăng", landmark.Province));
        Assert.Contains(result, landmark => landmark.Name == "Tân Huê Viên");
        Assert.Contains(result, landmark => landmark.Name == "Chợ nổi Ngã Năm");
        Assert.Contains(result, landmark => landmark.Name == "Chùa Dơi");
        Assert.Contains(result, landmark => landmark.Name == "Chùa Phật Học 2");
        Assert.Contains(result, landmark => landmark.Name == "Chùa La Hán");
        Assert.Contains(result, landmark => landmark.Name == "Khu căn cứ Tỉnh ủy Sóc Trăng");
    }

    [Fact]
    public void GetDanhSachDiaDiem_FiltersByCategoryAndSearch()
    {
        var result = _service.GetDanhSachDiaDiem("Ngã Năm", "Văn hóa", "vi");

        var landmark = Assert.Single(result);
        Assert.Equal("cho-noi-nga-nam", landmark.Id);
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("en")]
    [InlineData("fr")]
    public void GetDanhSachDiaDiem_ProvidesTranslationsForAllDestinations(string language)
    {
        var result = _service.GetDanhSachDiaDiem(null, null, language);

        Assert.Equal(6, result.Count);
        Assert.All(result, landmark => Assert.False(string.IsNullOrWhiteSpace(landmark.Description)));
    }

    [Fact]
    public void GetDanhSachDiaDiem_IncludesGalleryPhotosForEveryDestination()
    {
        var result = _service.GetDanhSachDiaDiem(null, null, "vi");

        Assert.All(result, place =>
        {
            Assert.NotEmpty(place.GalleryPhotos);
            Assert.All(place.GalleryPhotos, photo =>
            {
                Assert.False(string.IsNullOrWhiteSpace(photo.Url));
                Assert.False(string.IsNullOrWhiteSpace(photo.SourceUrl));
            });
            Assert.Equal(
                place.GalleryPhotos.Count,
                place.GalleryPhotos.Select(photo => photo.Url).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        });
    }

    [Fact]
    public void GetDiaDiem_ReturnsDescriptionInRequestedLanguage()
    {
        var result = _service.GetDiaDiem("cho-noi-nga-nam", "en");

        Assert.NotNull(result);
        Assert.Contains("five waterways", result.Description);
    }

    [Fact]
    public void GetDiaDiem_ReturnsNullWhenIdDoesNotExist()
    {
        var result = _service.GetDiaDiem("not-found", "vi");

        Assert.Null(result);
    }

    [Fact]
    public void TimViTri_ReturnsMapAndDirectionsForDestinationAndOrigin()
    {
        var timViTri = new TimViTri(new KhoDiaDiemThamQuanTrongBoNho());

        var result = timViTri.ThucHien("cho-noi-nga-nam", 5.0322, 105.7845);

        Assert.NotNull(result);
        Assert.Equal("Chợ nổi Ngã Năm", result.TenDiaDiem);
        Assert.Contains("maps.google.com/maps?saddr=5.0322%2C105.7845", result.BanDoUrl);
        Assert.Contains("&daddr=", result.BanDoUrl);
        Assert.Contains("&output=embed", result.BanDoUrl);
        Assert.Contains("destination=", result.ChiDuongUrl);
        Assert.Contains("origin=5.0322%2C105.7845", result.ChiDuongUrl);
    }

    [Fact]
    public void TimViTri_ReturnsNullWhenDestinationDoesNotExist()
    {
        var timViTri = new TimViTri(new KhoDiaDiemThamQuanTrongBoNho());

        var result = timViTri.ThucHien("not-found", null, null);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(91, 105)]
    [InlineData(5, 181)]
    [InlineData(double.NaN, 105)]
    [InlineData(5, double.PositiveInfinity)]
    public void TimViTri_RejectsInvalidCoordinates(double latitude, double longitude)
    {
        var timViTri = new TimViTri(new KhoDiaDiemThamQuanTrongBoNho());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            timViTri.ThucHien("cho-noi-nga-nam", latitude, longitude));
    }

    [Fact]
    public void TimViTri_RequiresBothCoordinatesTogether()
    {
        var timViTri = new TimViTri(new KhoDiaDiemThamQuanTrongBoNho());

        Assert.Throws<ArgumentException>(() =>
            timViTri.ThucHien("cho-noi-nga-nam", 5.0322, null));
    }

    [Fact]
    public void GetDanhSachDiaDiem_RejectsUnsupportedLanguage()
    {
        Assert.Throws<ArgumentException>(() => _service.GetDanhSachDiaDiem(null, null, "ja"));
    }

    [Theory]
    [InlineData("vi", "vi-VN")]
    [InlineData("EN", "en-US")]
    [InlineData("fr", "fr-FR")]
    public void ChonNgonNgu_ReturnsNormalizedSpeechLanguage(string language, string speechLanguage)
    {
        var result = new ChonNgonNgu().ThucHien(language);

        Assert.Equal(speechLanguage, result.MaGiongDoc);
    }

    [Fact]
    public void XemChiTiet_ReturnsSelectedLanguageNarration()
    {
        var language = new ChonNgonNgu();
        var xemDiaDiem = new XemDiaDiem(new KhoDiaDiemThamQuanTrongBoNho(), language);
        var result = new XemChiTiet(xemDiaDiem).ThucHien("cho-noi-nga-nam", "en");

        Assert.NotNull(result);
        Assert.Contains("five waterways", result.NoiDung);
    }

    [Fact]
    public void NgheNoiDung_ReturnsPlaceAndSpeechVoice()
    {
        var language = new ChonNgonNgu();
        var xemDiaDiem = new XemDiaDiem(new KhoDiaDiemThamQuanTrongBoNho(), language);
        var result = new NgheNoiDung(xemDiaDiem, language)
            .ThucHien("cho-noi-nga-nam", "fr");

        Assert.NotNull(result);
        Assert.Equal("fr-FR", result.MaGiongDoc);
        Assert.Equal("cho-noi-nga-nam", result.DiaDiem.Id);
    }

    [Theory]
    [InlineData(-3, 10, 0)]
    [InlineData(5, 10, 5)]
    [InlineData(12, 10, 10)]
    public void TamDung_ClampsPlaybackPosition(int position, int length, int expectedPosition)
    {
        var result = new TamDung().ThucHien(position, length);

        Assert.Equal(expectedPosition, result.ViTriKyTu);
    }

    [Fact]
    public void NgheLai_ResetsPlaybackPosition()
    {
        Assert.Equal(0, new NgheLai().ThucHien().ViTriKyTu);
    }
}
