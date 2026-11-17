# Đi & Kể — khám phá Sóc Trăng

Ứng dụng dành cho khách tham quan sáu điểm đến du lịch tại Sóc Trăng: Tân Huê Viên, chợ nổi Ngã Năm, chùa Dơi, chùa Phật Học 2, chùa La Hán và khu căn cứ Tỉnh ủy Sóc Trăng. Giao diện hỗ trợ tìm kiếm, lọc, lưu yêu thích và nghe thuyết minh bằng tiếng Việt, Anh hoặc Pháp.
Mỗi địa điểm có nút **Xem địa điểm** để mở bộ ảnh khung cảnh và nguồn ảnh tương ứng, cùng nút **Xem chi tiết nội dung** để đọc đầy đủ bài thuyết minh theo ngôn ngữ đang chọn.

## Kiến trúc 3 lớp

- **Lớp giao diện:** `MaNguon/LopGiaoDien` — ASP.NET Core Web API, REST controllers và giao diện tĩnh trong `wwwroot`.
- **Lớp nghiệp vụ:** `MaNguon/LopNghiepVu` — các ca sử dụng `TimDiaDiem`, `ChonNgonNgu`, `XemDiaDiem`, `XemChiTiet`, `NgheNoiDung`, `TamDung` và `NgheLai`; cùng DTO và hợp đồng repository.
- **Lớp truy cập dữ liệu:** `MaNguon/LopTruyCapDuLieu` — EF Core dùng bộ nhớ trong làm bộ đệm cho API; sáu bảng lõi trong SQL Server là nguồn dữ liệu chính và lưu CRUD của Admin.

Luồng chính: trình duyệt → controllers → từng ca sử dụng trong `LopNghiepVu/Services` → `VisitorGuideDbContext` → SQL Server. Trình duyệt vẫn thực thi phát âm thanh bằng Web Speech API; quy tắc chọn nội dung, ngôn ngữ và vị trí tạm dừng/nghe lại được xử lý tại lớp nghiệp vụ.

`Database/database.sql` định nghĩa đúng sáu bảng lõi: `Admin_Accounts`, `Audio_Files`, `Languages`, `Narrations`, `POIs` và `Translations`. Bốn cấu trúc dữ liệu giao diện tham quan (`DiaDiemThamQuan`, `MoTaDiaDiem`, `DiemNoiBatDiaDiem`, `AnhBoSungDiaDiem`) được ánh xạ vào các bảng này, không tạo thêm bảng SQL. CRUD của Admin được ghi trực tiếp vào các bảng liên quan; ứng dụng nạp lại dữ liệu SQL trước mỗi API request để thay đổi trực tiếp từ SQL Server cũng xuất hiện trên web. `Admin_Accounts.ApplicationDataJson` chỉ còn phục vụ nhập dữ liệu snapshot cũ một lần và lưu dấu mốc chuyển đổi.

## Chạy ứng dụng

Yêu cầu .NET 8 SDK:

```powershell
# Chạy từ thư mục gốc repository
dotnet restore .\KhachThamQuan\ThamQuanSocTrang.sln
dotnet run --project .\KhachThamQuan\MaNguon\LopGiaoDien\ThamQuanSocTrang.Web.csproj
```

Ứng dụng cần SQL Server và mặc định kết nối `.\SQLEXPRESS`, cơ sở dữ liệu `DuLichSocTrang` bằng Windows Authentication (có thể ghi đè bằng `ConnectionStrings__VisitorGuide`). Tài khoản cần quyền đọc/ghi sáu bảng và thêm các cột tương thích lần đầu chạy. Bảng `Admin_Accounts` phải có ít nhất một hàng. Khi chuyển từ bản cũ, ứng dụng nhập snapshot đã lưu vào các bảng một lần; những lần khởi động sau đọc dữ liệu từ SQL, không nạp lại dữ liệu mẫu. `Database/database.sql` dùng để tạo cơ sở dữ liệu mới; **không chạy lại trên cơ sở dữ liệu đang có dữ liệu** vì script xóa và tạo lại sáu bảng.

Khi chạy profile `http` mặc định trên chính máy tính, trang đăng nhập sẽ tự điền và hiển thị tài khoản thử nghiệm `Admin` / `123`. Tài khoản này chỉ được máy chủ chấp nhận trong môi trường `Development`, khi kết nối trực tiếp từ `localhost` và chưa cấu hình tài khoản khác. Profile LAN và môi trường Production không dùng được tài khoản demo.

### Mở QR công khai tạm thời

Các API quản trị và trang dashboard yêu cầu cookie đăng nhập phía máy chủ. Trước khi chạy, đặt tài khoản riêng trong PowerShell; không dùng lại mật khẩu ở dịch vụ khác:

```powershell
$env:AdminAuth__Username = "admin"
$securePassword = Read-Host "Nhập mật khẩu quản trị riêng" -AsSecureString
$env:AdminAuth__Password = (New-Object System.Net.NetworkCredential("", $securePassword)).Password
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run --project .\KhachThamQuan\MaNguon\LopGiaoDien\ThamQuanSocTrang.Web.csproj --no-launch-profile -- --urls http://127.0.0.1:5200
```

Trong terminal thứ hai, cài `cloudflared` nếu máy chưa có (mở terminal mới sau khi cài), rồi chạy tunnel:

```powershell
winget install --id Cloudflare.cloudflared
cloudflared tunnel --url http://127.0.0.1:5200
```

Mở URL HTTPS `trycloudflare.com` được in ra, đăng nhập tại `/admin_login.html`, rồi mở QR trong dashboard. QR tự dùng domain của tunnel. Tunnel chỉ hoạt động khi cả ứng dụng và `cloudflared` còn chạy; URL có thể đổi sau khi khởi động lại. Tunnel là dịch vụ trung gian bên thứ ba, nên chỉ dùng mật khẩu tạm riêng và không đưa dữ liệu nhạy cảm lên đó.

### Mở mã QR trên điện thoại cùng Wi-Fi

Profile mặc định chỉ nghe trên `localhost`, vì vậy điện thoại không thể truy cập địa chỉ đó. Để thử trên cùng mạng Wi-Fi, chạy profile LAN:

```powershell
dotnet run --project .\KhachThamQuan\MaNguon\LopGiaoDien\ThamQuanSocTrang.Web.csproj --launch-profile LAN
```

Chạy profile `http` mặc định, rồi mở `http://localhost:5200/admin_dashboard.html` trên máy chạy ứng dụng. Profile này nghe cùng cổng 5200 trên máy tính và các card mạng; QR dùng cùng trang gốc `/` và cổng 5200 nhưng thay `localhost` bằng địa chỉ Wi-Fi/LAN để thiết bị khác truy cập. Hộp thoại QR dò các địa chỉ IPv4 đang hoạt động, ưu tiên Wi-Fi; nếu có nhiều card mạng, chọn địa chỉ cùng mạng với điện thoại. Windows Firewall phải cho phép ứng dụng trên mạng riêng tin cậy. Nếu chỉ dùng profile `LAN`, mở Admin tại `http://localhost:5201/admin_dashboard.html`.

Để khách ở mạng khác truy cập, ứng dụng phải được đưa ra Internet qua domain/hosting hoặc tunnel công khai; QR không thể làm địa chỉ localhost/LAN truy cập được từ Internet. Có thể đặt URL công khai trước khi chạy ứng dụng:

```powershell
$env:PublicBaseUrl = "https://ten-mien-cong-khai-cua-ban"
dotnet run --project .\KhachThamQuan\MaNguon\LopGiaoDien\ThamQuanSocTrang.Web.csproj
```

Khi cấu hình `PublicBaseUrl`, QR luôn dùng URL đó kể cả khi Admin mở dashboard bằng localhost. Nếu không cấu hình, QR dùng domain hiện tại hoặc địa chỉ LAN được dò tự động. Tunnel công khai là dịch vụ bên thứ ba; chỉ dùng URL tin cậy và thông tin đăng nhập Admin riêng.

Số online được tính theo trình duyệt/thiết bị đang gửi heartbeat; nhiều tab cùng trình duyệt chỉ tính một, đóng tab cuối sẽ giảm ngay khi trình duyệt gửi tín hiệu, và tự hết hạn sau 45 giây nếu thiết bị mất mạng/đóng đột ngột. Không thể xác định chính xác một người thật nếu họ dùng nhiều thiết bị mà không đăng nhập, nên điện thoại và máy tính của cùng một người vẫn được tính riêng. Lượt nghe được cộng trực tiếp vào `POIs.PlayCount` trong SQL, không bị reset khi khởi động lại ứng dụng.

Lần khởi động đầu, ứng dụng chuyển snapshot cũ (nếu có) sang các bảng hiện hữu; nếu chưa có dữ liệu, ứng dụng nạp địa điểm ban đầu từ `App_Data/places.json` (hoặc `SeedData/places.json`) và trạng thái Admin từ `admin_state.json`. Khi chạy, ứng dụng chỉ bổ sung các cột còn thiếu; không xóa hoặc tạo thêm bảng. Sao lưu cơ sở dữ liệu để giữ các thay đổi qua dashboard.

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
