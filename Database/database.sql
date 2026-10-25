-- =========================================================
-- DATABASE CHÍNH THỨC: HỆ THỐNG THUYẾT MINH DU LỊCH SÓC TRĂNG
-- =========================================================

-- 1. TẠO DATABASE MỚI TINH (Tên chuẩn: DuLichSocTrang)
CREATE DATABASE DuLichSocTrang;
GO
USE DuLichSocTrang;
GO

-- 2. TẠO CẤU TRÚC CÁC BẢNG (Đã sửa lỗi nvarchar chuẩn)
CREATE TABLE [Roles] (
  [RoleID] int PRIMARY KEY,
  [RoleName] nvarchar(50)
)
GO

CREATE TABLE [Users] (
  [UserID] int PRIMARY KEY,
  [RoleID] int,
  [FullName] nvarchar(100),
  [Email] nvarchar(255),
  [PasswordHash] nvarchar(255)
)
GO

CREATE TABLE [POIs] (
  [PoiID] int PRIMARY KEY,
  [UserID] int,
  [OriginalName] nvarchar(255),
  [OriginalDescription] nvarchar(MAX),
  [Latitude] float,
  [Longitude] float
)
GO

CREATE TABLE [POI_Images] (
  [ImageID] int PRIMARY KEY,
  [PoiID] int,
  [ImageURL] nvarchar(255)
)
GO

CREATE TABLE [Languages] (
  [LangCode] nvarchar(50) PRIMARY KEY,
  [LangName] nvarchar(100)
)
GO

CREATE TABLE [POI_Localizations] (
  [LocalizationID] int PRIMARY KEY,
  [PoiID] int,
  [LangCode] nvarchar(50),
  [TranslatedName] nvarchar(255),
  [TranslatedDescription] nvarchar(MAX),
  [AudioURL] nvarchar(255)
)
GO

CREATE TABLE [Audio_Histories] (
  [HistoryID] int PRIMARY KEY,
  [PoiID] int,
  [LangCode] nvarchar(50),
  [CreatedDate] datetime,
  [Status] nvarchar(50)
)
GO

-- 3. TẠO KHÓA NGOẠI
ALTER TABLE [Users] ADD FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID])
ALTER TABLE [POIs] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
ALTER TABLE [POI_Images] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO

-- 4. NẠP DỮ LIỆU QUẢN TRỊ & NGÔN NGỮ
INSERT INTO [Roles] VALUES (1, N'Admin'), (2, N'User');
INSERT INTO [Users] VALUES 
(1, 1, N'Quản trị viên', 'admin@soctrang.vn', 'hash1234'), 
(2, 2, N'Khách tham quan', 'user@gmail.com', 'hash5678');
INSERT INTO [Languages] VALUES ('vi', N'Tiếng Việt'), ('en', N'English'), ('zh', N'Tiếng Trung');
GO

-- 5. NẠP CHUẨN 5 ĐỊA ĐIỂM SÓC TRĂNG
INSERT INTO [POIs] ([PoiID], [UserID], [OriginalName], [OriginalDescription], [Latitude], [Longitude]) VALUES 
(1, 1, N'Tân Huê Viên', N'Hoạt động', 9.6133, 105.9922),
(2, 1, N'Chợ nổi ngã 5', N'Hoạt động', 9.5761, 105.8117),
(3, 1, N'Chùa Phật Học 2', N'Hoạt động', 9.6056, 105.9754),
(4, 1, N'Chùa La Hán', N'Bảo trì', 9.6080, 105.9860),
(5, 1, N'Khu căn cứ tỉnh ủy Sóc Trăng', N'Hoạt động', 9.5397, 105.9768);
GO

-- 6. NẠP NỘI DUNG THUYẾT MINH MẪU
INSERT INTO [POI_Localizations] ([LocalizationID], [PoiID], [LangCode], [TranslatedName], [TranslatedDescription], [AudioURL]) VALUES 
(1, 1, 'vi', N'Khu du lịch Tân Huê Viên', N'Nơi sản xuất bánh Pía Sóc Trăng nổi tiếng với kiến trúc Liên Hoa Bảo Tháp.', 'tanhuevien_vi.mp3'),
(2, 2, 'vi', N'Chợ nổi Ngã Năm', N'Giao điểm của 5 nhánh sông đi 5 ngả, nét văn hóa đặc trưng miền Tây.', 'nganam_vi.mp3'),
(3, 3, 'vi', N'Chùa Phật Học 2', N'Ngôi chùa có diện tích lớn nhất Sóc Trăng.', 'chuaphathoc2_vi.mp3'),
(4, 4, 'vi', N'Chùa La Hán', N'Ngôi chùa của người Hoa nổi tiếng với kiến trúc đá độc đáo.', 'chualahan_vi.mp3'),
(5, 5, 'vi', N'Khu căn cứ Tỉnh ủy', N'Di tích lịch sử quốc gia nằm giữa rừng tràm.', 'khucancu_vi.mp3');
GO

-- 7. GIẢ LẬP VÀI LƯỢT NGHE ĐỂ WEB ADMIN CÓ SỐ
INSERT INTO [Audio_Histories] ([HistoryID], [PoiID], [LangCode], [CreatedDate], [Status]) VALUES 
(1, 1, 'vi', GETDATE(), N'Hoàn thành'), (2, 1, 'vi', GETDATE(), N'Hoàn thành'), 
(3, 2, 'vi', GETDATE(), N'Hoàn thành'), (4, 3, 'vi', GETDATE(), N'Hoàn thành'), 
(5, 5, 'vi', GETDATE(), N'Hoàn thành');
GO

-- =========================================================
-- 8. XEM THỬ BẢNG DỮ LIỆU (SHOW TABLES)
-- =========================================================
SELECT * FROM [Users];
SELECT * FROM [POIs];
SELECT * FROM [POI_Localizations];
SELECT * FROM [Audio_Histories];
GO
