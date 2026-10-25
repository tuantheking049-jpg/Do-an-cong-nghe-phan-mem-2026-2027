namespace VisitorGuide.Application.Services;

using System.Globalization;
using VisitorGuide.Application.Repositories;

public sealed class TimViTri(IDiaDiemThamQuanRepository repository)
{
    public ViTriDiaDiemDto? ThucHien(string id, double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Vĩ độ và kinh độ phải được cung cấp cùng nhau.");
        }

        if (latitude is { } lat
            && (!double.IsFinite(lat) || lat is < -90 or > 90))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Vĩ độ không hợp lệ.");
        }

        if (longitude is { } lon
            && (!double.IsFinite(lon) || lon is < -180 or > 180))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Kinh độ không hợp lệ.");
        }

        var place = repository.GetById(id);
        if (place is null)
        {
            return null;
        }

        var destination = Uri.EscapeDataString($"{place.Name}, {place.Province}, Việt Nam");
        var mapUrl = $"https://maps.google.com/maps?q={destination}&output=embed";
        var directionsUrl = $"https://www.google.com/maps/dir/?api=1&destination={destination}";

        if (latitude is { } originLatitude && longitude is { } originLongitude)
        {
            var origin = string.Create(
                CultureInfo.InvariantCulture,
                $"{originLatitude:R},{originLongitude:R}");
            directionsUrl += $"&origin={Uri.EscapeDataString(origin)}";
        }

        return new ViTriDiaDiemDto(place.Name, mapUrl, directionsUrl);
    }
}

/// <summary>Thông tin bản đồ và liên kết chỉ đường đến địa điểm tham quan.</summary>
public sealed record ViTriDiaDiemDto(string TenDiaDiem, string BanDoUrl, string ChiDuongUrl);
