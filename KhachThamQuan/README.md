# Đi & Kể — khám phá Sóc Trăng

Ứng dụng dành cho khách tham quan sáu điểm đến du lịch tại Sóc Trăng: Tân Huê Viên, chợ nổi Ngã Năm, chùa Dơi, chùa Phật Học 2, chùa La Hán và khu căn cứ Tỉnh ủy Sóc Trăng. Giao diện hỗ trợ tìm kiếm, lọc, lưu yêu thích và nghe thuyết minh bằng tiếng Việt, Anh hoặc Pháp.
Mỗi địa điểm có nút **Xem địa điểm** để mở bộ ảnh khung cảnh và nguồn ảnh tương ứng, cùng nút **Xem chi tiết nội dung** để đọc đầy đủ bài thuyết minh theo ngôn ngữ đang chọn.

## Kiến trúc 3 lớp

- **Lớp giao diện:** `MaNguon/LopGiaoDien` — ASP.NET Core Web API, REST controllers và giao diện tĩnh trong `wwwroot`.
- **Lớp nghiệp vụ:** `MaNguon/LopNghiepVu` — các ca sử dụng `TimDiaDiem`, `ChonNgonNgu`, `XemDiaDiem`, `XemChiTiet`, `NgheNoiDung`, `TamDung` và `NgheLai`; cùng DTO và hợp đồng repository.
- **Lớp truy cập dữ liệu:** `MaNguon/LopTruyCapDuLieu` — EF Core repository kết nối SQL Server. `SeedData/places.json` chỉ dùng để nạp sáu địa điểm lần đầu vào database; ứng dụng sau đó đọc dữ liệu từ SQL Server.

Luồng chính: trình duyệt → controllers → từng ca sử dụng trong `LopNghiepVu/Services` → `Repositories/IDiaDiemThamQuanRepository.cs` → `Repositories/SqlServerDiaDiemThamQuanRepository.cs` → SQL Server. Trình duyệt vẫn thực thi phát âm thanh bằng Web Speech API; quy tắc chọn nội dung, ngôn ngữ và vị trí tạm dừng/nghe lại được xử lý tại lớp nghiệp vụ.

Cơ sở dữ liệu là `DuLichSocTrang`, gồm 6 bảng ứng với 6 kho dữ liệu trong DFD: `Admin_Accounts`, `Languages`, `POIs`, `Narrations`, `Translations`, `Audio_Files`. Schema và dữ liệu mẫu nằm trong `Database/database_v2.sql` (chạy script này trong SSMS trước khi chạy ứng dụng); `DatabaseInitializer` chỉ kiểm tra kết nối, không tự tạo bảng. Trang người dùng đọc `POIs` + `Narrations` (ngôn ngữ gốc) + `Translations` (các ngôn ngữ còn lại); trang Admin ghi vào chính các bảng đó và đăng nhập bằng `Admin_Accounts`. Điểm nổi bật và thư viện ảnh lưu dạng JSON trong cột `POIs.HighlightsJson` và `POIs.GalleryJson`. Chuỗi kết nối mặc định trong `appsettings.json` dùng SQL Server cục bộ với Windows Authentication; có thể thay bằng instance khác qua biến môi trường `ConnectionStrings__VisitorGuide`, không đưa mật khẩu thật vào source control. `SeedData/places.json` chỉ còn dùng cho kho dữ liệu trong bộ nhớ của dự án kiểm thử.

## Chạy ứng dụng

Yêu cầu .NET 8 SDK:

```powershell
# Chạy từ thư mục gốc repository
dotnet restore .\KhachThamQuan\ThamQuanSocTrang.sln
dotnet run --project .\KhachThamQuan\MaNguon\LopGiaoDien\ThamQuanSocTrang.Web.csproj
```

Cần cài và chạy SQL Server Express hoặc SQL Server. Nếu dùng instance khác, cấu hình `ConnectionStrings:VisitorGuide`, ví dụ trong PowerShell:

```powershell
$env:ConnectionStrings__VisitorGuide = "Server=localhost;Database=DuLichSocTrang;Trusted_Connection=True;TrustServerCertificate=True"
```

Trước lần chạy đầu, mở `Database/database_v2.sql` trong SSMS và chạy toàn bộ để tạo database `DuLichSocTrang`. Tài khoản admin mẫu: `admin` / `123` (mật khẩu lưu dạng SHA-256 trong `Admin_Accounts.PasswordHash`).

`global.json` ở gốc repository chọn .NET SDK 8 khi chạy lệnh từ VS Code hoặc terminal ở bất kỳ thư mục con nào trong repository.
Chọn `ThamQuanSocTrang.Web` làm startup project trong IDE. `LopNghiepVu` và `LopTruyCapDuLieu` là thư viện lớp, không thể chạy trực tiếp. Mở URL được in ra trong terminal để xem giao diện; Swagger ở `/swagger`.

### API

- `GET /api/landmarks?language=vi&search=chùa&category=Kiến%20trúc`
- `GET /api/landmarks/cho-noi-nga-nam?language=en`
- `GET /api/landmarks/cho-noi-nga-nam/details?language=vi`
- `GET /api/narration/cho-noi-nga-nam?language=vi`
- `GET /api/languages/en`
- `POST /api/narration/pause`
- `POST /api/narration/replay`
- `GET /health`

Ngôn ngữ: `vi`, `en`, `fr`. Bộ đọc thuyết minh sử dụng Web Speech API của trình duyệt; khả năng phát giọng nói tùy thuộc hệ điều hành và trình duyệt.
Lệnh tạm dừng nhận vị trí hiện tại và độ dài nội dung, ví dụ `{"viTriKyTu":120,"doDaiNoiDung":850}`; lớp nghiệp vụ kiểm tra vị trí trước khi trình duyệt tạm dừng phát.

### Ảnh địa điểm và ghi công

Ảnh trên thẻ, ảnh bìa và banner được chọn theo đúng sáu địa điểm Sóc Trăng; nhấn **Nguồn ảnh** trên từng thẻ để mở trang nguồn. Ba ảnh chùa từ Wikimedia Commons được ghi công theo giấy phép:

- Chùa Dơi — Mai Lan, [CC BY-SA 2.0](https://creativecommons.org/licenses/by-sa/2.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_(40553739771).jpg).
- Chùa Phật Học 2 — Phucphucphuc, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg).
- Chùa La Hán — Phucphucphuc, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), [trang ảnh](https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg).

## CI/CD

`.github/workflows/ci-cd-tham-quan-soc-trang.yml` chạy restore, build, unit tests và publish cho pull request và push lên `main`. Job deploy đưa artifact lên Azure App Service khi push lên `main`, nếu đã cấu hình:

- Repository/environment variable `AZURE_WEBAPP_NAME`.
- Repository/environment secret `AZURE_WEBAPP_PUBLISH_PROFILE`.

Không có thông tin xác thực thì CI vẫn build, test và tạo artifact; bước deploy được bỏ qua.
