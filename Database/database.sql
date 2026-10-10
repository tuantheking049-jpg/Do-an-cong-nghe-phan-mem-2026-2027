-- 1. TẠO LẠI DATABASE MỚI
CREATE DATABASE HuongDanDuLichQ4;
GO
USE HuongDanDuLichQ4;
GO

-- 2. TẠO CÁC BẢNG
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

-- 3. TẠO KHÓA NGOẠI (MỐI QUAN HỆ)
ALTER TABLE [Users] ADD FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID])
ALTER TABLE [POIs] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
ALTER TABLE [POI_Images] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO

-- 4. CHÈN DỮ LIỆU MẪU 
INSERT INTO [Roles] VALUES (1, N'Admin'), (2, N'User');
INSERT INTO [Users] VALUES 
(1, 1, N'Thanh Vien 1', 'admin@gmail.com', 'hash1234'), 
(2, 2, N'Thanh Vien 2', 'user@gmail.com', 'hash5678');
INSERT INTO [Languages] VALUES ('vi', N'Tiếng Việt'), ('en', N'English');
INSERT INTO [POIs] VALUES (1, 1, N'Bến Nhà Rồng', N'Bảo tàng Hồ Chí Minh tại Quận 4', 10.7681, 106.7068);
INSERT INTO [POI_Images] VALUES (1, 1, 'ben_nha_rong_1.jpg');
INSERT INTO [POI_Localizations] VALUES (1, 1, 'en', N'Dragon House Wharf', N'Ho Chi Minh Museum in District 4', 'audio_en_1.mp3');
GO

SELECT * FROM Users;
SELECT * FROM POIs;
SELECT * FROM POI_Localizations;