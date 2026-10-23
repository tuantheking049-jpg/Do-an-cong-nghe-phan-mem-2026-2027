using VisitorGuide.Application;
using VisitorGuide.Application.Services;
using VisitorGuide.Infrastructure.Repositories;

namespace VisitorGuide.Application.Tests;

public sealed class VisitorGuideServiceTests
{
    private readonly VisitorGuideService _service = new(new InMemoryLandmarkRepository());

    [Fact]
    public void GetLandmarks_ReturnsOnlyTheSixSocTrangDestinations()
    {
        var result = _service.GetLandmarks(null, null, "vi");

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
    public void GetLandmarks_FiltersByCategoryAndSearch()
    {
        var result = _service.GetLandmarks("Ngã Năm", "Văn hóa", "vi");

        var landmark = Assert.Single(result);
        Assert.Equal("cho-noi-nga-nam", landmark.Id);
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("en")]
    [InlineData("fr")]
    public void GetLandmarks_ProvidesTranslationsForAllDestinations(string language)
    {
        var result = _service.GetLandmarks(null, null, language);

        Assert.Equal(6, result.Count);
        Assert.All(result, landmark => Assert.False(string.IsNullOrWhiteSpace(landmark.Description)));
    }

    [Fact]
    public void GetLandmark_ReturnsDescriptionInRequestedLanguage()
    {
        var result = _service.GetLandmark("cho-noi-nga-nam", "en");

        Assert.NotNull(result);
        Assert.Contains("five waterways", result.Description);
    }

    [Fact]
    public void GetLandmark_ReturnsNullWhenIdDoesNotExist()
    {
        var result = _service.GetLandmark("not-found", "vi");

        Assert.Null(result);
    }

    [Fact]
    public void GetLandmarks_RejectsUnsupportedLanguage()
    {
        Assert.Throws<ArgumentException>(() => _service.GetLandmarks(null, null, "ja"));
    }
}
