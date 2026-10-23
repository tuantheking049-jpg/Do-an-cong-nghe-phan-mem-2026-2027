# Đi & Kể — thuyết minh tự động đa ngôn ngữ

Ứng dụng thuyết minh dành cho khách tham quan sáu điểm đến tại Sóc Trăng: Tân Huê Viên, chợ nổi Ngã Năm, chùa Dơi, chùa Phật Học 2, chùa La Hán và khu căn cứ Tỉnh ủy Sóc Trăng. Giao diện hỗ trợ tìm kiếm, lọc, lưu yêu thích và nghe thuyết minh bằng tiếng Việt, Anh hoặc Pháp.

## Kiến trúc 3 lớp

- **Lớp giao diện:** `MaNguon/LopGiaoDien` — ASP.NET Core Web API, REST controllers và giao diện tĩnh trong `TaiNguyenTinh`.
- **Lớp nghiệp vụ:** `MaNguon/LopNghiepVu` — nghiệp vụ tìm kiếm, chọn bản dịch và DTO; có `Services` và `Repositories` (hợp đồng truy cập dữ liệu).
- **Lớp truy cập dữ liệu:** `MaNguon/LopTruyCapDuLieu/Repositories` — repository dữ liệu mẫu trong bộ nhớ cho sáu địa điểm Sóc Trăng, có thể thay bằng EF Core/SQL Server.

Luồng chính: trình duyệt → `Controllers/LandmarksController.cs` → `Services/VisitorGuideService.cs` → `Repositories/ILandmarkRepository.cs` → `LopTruyCapDuLieu/Repositories/InMemoryLandmarkRepository.cs`.

## Chạy ứng dụng

Yêu cầu .NET 8 SDK:

```powershell
dotnet restore GiaiPhapThuyetMinh.sln
dotnet run --project MaNguon/LopGiaoDien/LopGiaoDien.csproj
```

Mở URL được in ra trong terminal. Swagger bật trong môi trường Development; giao diện khách tham quan được phục vụ tại `/`.

### API

- `GET /api/landmarks?language=vi&search=chùa&category=Kiến%20trúc`
- `GET /api/landmarks/cho-noi-nga-nam?language=en`
- `GET /health`

Ngôn ngữ: `vi`, `en`, `fr`. Bộ đọc thuyết minh sử dụng Web Speech API của trình duyệt; khả năng phát giọng nói tùy thuộc hệ điều hành và trình duyệt.

### Ảnh địa điểm và ghi công

Ảnh trên thẻ, ảnh bìa và banner được chọn theo đúng sáu địa điểm Sóc Trăng; nhấn **Nguồn ảnh** trên từng thẻ để mở trang nguồn. Ba ảnh chùa từ Wikimedia Commons được ghi công theo giấy phép:

- Chùa Dơi — Mai Lan, [CC BY-SA 2.0](https://creativecommons.org/licenses/by-sa/2.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_(40553739771).jpg).
- Chùa Phật Học 2 — Phucphucphuc, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg).
- Chùa La Hán — Phucphucphuc, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg).

## CI/CD

`.github/workflows/tich-hop-trien-khai.yml` chạy restore, build, unit tests và publish cho pull request và push lên `main`. Job deploy đưa artifact lên Azure App Service khi push lên `main`, nếu đã cấu hình:

- Repository/environment variable `AZURE_WEBAPP_NAME`.
- Repository/environment secret `AZURE_WEBAPP_PUBLISH_PROFILE`.

Không có thông tin xác thực thì CI vẫn build, test và tạo artifact; bước deploy được bỏ qua.
