IF DB_ID(N'DuLichSocTrang') IS NULL CREATE DATABASE DuLichSocTrang;
GO
USE DuLichSocTrang;
GO
DROP TABLE IF EXISTS Audio_Files, Translations, Narrations, POIs, Languages, Admin_Accounts;
GO

CREATE TABLE Admin_Accounts (
    AdminID      INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    IsLocked     BIT NOT NULL DEFAULT 0,
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE(),
    ApplicationDataJson NVARCHAR(MAX) NULL
);

CREATE TABLE Languages (
    LangCode     NVARCHAR(10)  PRIMARY KEY,
    LangName     NVARCHAR(100) NOT NULL,
    IsEnabled    BIT NOT NULL DEFAULT 1,
    VoiceLocale  NVARCHAR(35) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE POIs (
    PoiID        INT IDENTITY(1,1) PRIMARY KEY,
    Slug         NVARCHAR(100) NOT NULL UNIQUE,
    Name         NVARCHAR(255) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    Category     NVARCHAR(100) NULL,
    Province     NVARCHAR(150) NULL,
    Address      NVARCHAR(255) NULL,
    ImageUrl     NVARCHAR(2048) NULL,
    Latitude     FLOAT NULL,
    Longitude    FLOAT NULL,
    Status       NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động',
    DisplayOrder INT NOT NULL DEFAULT 0,
    DurationMinutes INT NOT NULL DEFAULT 5,
    Rating          FLOAT NOT NULL DEFAULT 0,
    ReviewCount     INT NOT NULL DEFAULT 0,
    PlayCount       INT NOT NULL DEFAULT 0,
    ImageSourceUrl  NVARCHAR(2048) NULL,
    ImagePosition   NVARCHAR(100) NULL,
    HighlightsJson  NVARCHAR(MAX) NULL,
    GalleryJson     NVARCHAR(MAX) NULL,
    CreatedBy    INT NULL REFERENCES Admin_Accounts(AdminID)
);

CREATE TABLE Narrations (
    NarrationID   INT IDENTITY(1,1) PRIMARY KEY,
    PoiID         INT NOT NULL REFERENCES POIs(PoiID) ON DELETE CASCADE,
    SourceLang    NVARCHAR(10) NOT NULL DEFAULT 'vi' REFERENCES Languages(LangCode),
    Title         NVARCHAR(255) NOT NULL,
    Content       NVARCHAR(MAX) NULL,
    PublishStatus NVARCHAR(30) NOT NULL DEFAULT N'Bản nháp',
    UpdatedBy     INT NULL REFERENCES Admin_Accounts(AdminID),
    UpdatedAt     DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Narration_Poi UNIQUE (PoiID)
);

CREATE TABLE Translations (
    TranslationID     INT IDENTITY(1,1) PRIMARY KEY,
    NarrationID       INT NOT NULL REFERENCES Narrations(NarrationID) ON DELETE CASCADE,
    LangCode          NVARCHAR(10) NOT NULL REFERENCES Languages(LangCode),
    TranslatedContent NVARCHAR(MAX) NOT NULL,
    Status            NVARCHAR(30) NOT NULL DEFAULT N'Đã công bố',
    DisplayOrder      INT NOT NULL DEFAULT 0,
    UpdatedAt         DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Translation UNIQUE (NarrationID, LangCode)
);

CREATE TABLE Audio_Files (
    AudioID       INT IDENTITY(1,1) PRIMARY KEY,
    NarrationID   INT NOT NULL REFERENCES Narrations(NarrationID) ON DELETE CASCADE,
    LangCode      NVARCHAR(10) NOT NULL REFERENCES Languages(LangCode),
    VoiceName     NVARCHAR(100) NULL,
    SourceType    NVARCHAR(10) NOT NULL DEFAULT 'TTS' CHECK (SourceType IN ('TTS','Upload')),
    AudioURL      NVARCHAR(MAX) NOT NULL,
    SpeechRate    FLOAT NOT NULL DEFAULT 1.0,
    Pitch         FLOAT NOT NULL DEFAULT 1.0,
    Status        NVARCHAR(30) NOT NULL DEFAULT N'Đã công bố',
    DisplayOrder  INT NOT NULL DEFAULT 0,
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
GO
CREATE INDEX IX_POIs_Name ON POIs(Name);
CREATE INDEX IX_POIs_Geo  ON POIs(Latitude, Longitude);
GO

INSERT INTO Admin_Accounts (Username, FullName, Email, PasswordHash)
VALUES (N'admin', N'Quản trị viên', N'admin@soctrang.vn', N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3');

INSERT INTO Languages (LangCode, LangName, IsEnabled, VoiceLocale, DisplayOrder) VALUES
('vi', N'Tiếng Việt', 1, N'vi-VN', 1),
('en', N'English', 1, N'en-US', 2),
('fr', N'Français', 1, N'fr-FR', 3);

INSERT INTO POIs (Slug, Name, Description, Category, Province, Address, ImageUrl, Latitude, Longitude, Status, DisplayOrder, CreatedBy) VALUES
(N'cho-noi-nga-nam', N'Chợ nổi Ngã Năm', N'Chợ nổi Ngã Năm họp tại nơi năm nhánh sông gặp nhau, tạo nên khung cảnh mua bán tấp nập trên ghe thuyền. Hãy đến từ sáng sớm để cảm nhận nhịp sống sông nước và nét giao thương đặc trưng của miền Tây.', N'Văn hóa', N'Sóc Trăng', N'Phường 1, thị xã Ngã Năm, tỉnh Sóc Trăng', N'https://images.vietnamtourism.gov.vn/vn/images/2014/AnhInternet/chonoinga5.jpg', 9.5761, 105.8117, N'Hoạt động', 1, 1),
(N'chua-doi', N'Chùa Dơi', N'Chùa Dơi, còn được biết đến với tên chùa Mahatup, là ngôi chùa Khmer Nam tông nổi tiếng ở Sóc Trăng. Ngôi chùa gây ấn tượng bởi kiến trúc Khmer và khuôn viên nhiều cây xanh, nơi đàn dơi thường trú ngụ trên những tán cây.', N'Kiến trúc', N'Sóc Trăng', N'Đường Văn Ngọc Chính, phường 3, thành phố Sóc Trăng', N'https://upload.wikimedia.org/wikipedia/commons/thumb/2/2c/Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_%2840553739771%29.jpg/960px-Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_%2840553739771%29.jpg', 9.5790, 105.9724, N'Hoạt động', 2, 1),
(N'chua-la-han', N'Chùa La Hán', N'Chùa La Hán là ngôi chùa gắn với cộng đồng người Hoa ở Sóc Trăng. Ghé thăm để khám phá những chi tiết kiến trúc nhiều màu sắc, không gian thờ tự trang nghiêm và dấu ấn giao thoa văn hóa của vùng đất này.', N'Kiến trúc', N'Sóc Trăng', N'Phường 8, thành phố Sóc Trăng', N'https://upload.wikimedia.org/wikipedia/commons/thumb/5/50/Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg/960px-Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg', 9.608, 105.986, N'Bảo trì', 3, 1),
(N'chua-phat-hoc-2', N'Chùa Phật Học 2', N'Chùa Phật Học 2 là điểm đến tâm linh được nhiều người ghé thăm tại thành phố Sóc Trăng. Dành chút thời gian dạo quanh khuôn viên rộng rãi, chiêm ngưỡng các công trình Phật giáo và tìm một khoảng lặng giữa hành trình.', N'Kiến trúc', N'Sóc Trăng', N'Phường 8, thành phố Sóc Trăng', N'https://upload.wikimedia.org/wikipedia/commons/thumb/5/50/Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg/960px-Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg', 9.6056, 105.9754, N'Hoạt động', 4, 1),
(N'khu-can-cu-tinh-uy-soc-trang', N'Khu căn cứ Tỉnh ủy Sóc Trăng', N'Khu căn cứ Tỉnh ủy Sóc Trăng tại rừng Mỹ Phước gắn liền với hoạt động cách mạng của địa phương trong thời kỳ kháng chiến. Tham quan khu di tích giúp bạn tìm hiểu thêm về lịch sử đấu tranh và cuộc sống của cán bộ trong căn cứ.', N'Lịch sử', N'Sóc Trăng', N'Rừng tràm Mỹ Phước, ấp Phước An B, xã Mỹ Phước, huyện Mỹ Tú', N'https://media-cdn-v2.laodong.vn/storage/newsportal/2025/4/20/1494178/Soc-Trang-1-01.jpg', 9.5397, 105.9768, N'Hoạt động', 5, 1),
(N'tan-hue-vien', N'Tân Huê Viên', N'Tân Huê Viên là điểm dừng chân gắn với đặc sản Sóc Trăng, đặc biệt là bánh pía. Ghé thăm để tìm hiểu thêm về món bánh nhiều lớp nhân đậu xanh, sầu riêng và trứng muối, một hương vị quen thuộc của miền Tây.', N'Ẩm thực', N'Sóc Trăng', N'153 Quốc lộ 1A, xã An Hiệp, huyện Châu Thành', N'https://www.dacsanbanhpia.com/wp-content/uploads/2015/05/banh-pia-tan-hue-vien.jpg', 9.6133, 105.9922, N'Hoạt động', 6, 1);

INSERT INTO Narrations (PoiID, SourceLang, Title, Content, PublishStatus, UpdatedBy) VALUES
((SELECT PoiID FROM POIs WHERE Slug = N'cho-noi-nga-nam'), 'vi', N'Thuyết minh Chợ nổi Ngã Năm', N'Chợ nổi Ngã Năm họp tại nơi năm nhánh sông gặp nhau, tạo nên khung cảnh mua bán tấp nập trên ghe thuyền. Hãy đến từ sáng sớm để cảm nhận nhịp sống sông nước và nét giao thương đặc trưng của miền Tây.', N'Đã công bố', 1),
((SELECT PoiID FROM POIs WHERE Slug = N'chua-doi'), 'vi', N'Thuyết minh Chùa Dơi', N'Chùa Dơi, còn được biết đến với tên chùa Mahatup, là ngôi chùa Khmer Nam tông nổi tiếng ở Sóc Trăng. Ngôi chùa gây ấn tượng bởi kiến trúc Khmer và khuôn viên nhiều cây xanh, nơi đàn dơi thường trú ngụ trên những tán cây.', N'Đã công bố', 1),
((SELECT PoiID FROM POIs WHERE Slug = N'chua-la-han'), 'vi', N'Thuyết minh Chùa La Hán', N'Chùa La Hán là ngôi chùa gắn với cộng đồng người Hoa ở Sóc Trăng. Ghé thăm để khám phá những chi tiết kiến trúc nhiều màu sắc, không gian thờ tự trang nghiêm và dấu ấn giao thoa văn hóa của vùng đất này.', N'Đã công bố', 1),
((SELECT PoiID FROM POIs WHERE Slug = N'chua-phat-hoc-2'), 'vi', N'Thuyết minh Chùa Phật Học 2', N'Chùa Phật Học 2 là điểm đến tâm linh được nhiều người ghé thăm tại thành phố Sóc Trăng. Dành chút thời gian dạo quanh khuôn viên rộng rãi, chiêm ngưỡng các công trình Phật giáo và tìm một khoảng lặng giữa hành trình.', N'Đã công bố', 1),
((SELECT PoiID FROM POIs WHERE Slug = N'khu-can-cu-tinh-uy-soc-trang'), 'vi', N'Thuyết minh Khu căn cứ Tỉnh ủy Sóc Trăng', N'Khu căn cứ Tỉnh ủy Sóc Trăng tại rừng Mỹ Phước gắn liền với hoạt động cách mạng của địa phương trong thời kỳ kháng chiến. Tham quan khu di tích giúp bạn tìm hiểu thêm về lịch sử đấu tranh và cuộc sống của cán bộ trong căn cứ.', N'Đã công bố', 1),
((SELECT PoiID FROM POIs WHERE Slug = N'tan-hue-vien'), 'vi', N'Thuyết minh Tân Huê Viên', N'Tân Huê Viên là điểm dừng chân gắn với đặc sản Sóc Trăng, đặc biệt là bánh pía. Ghé thăm để tìm hiểu thêm về món bánh nhiều lớp nhân đậu xanh, sầu riêng và trứng muối, một hương vị quen thuộc của miền Tây.', N'Đã công bố', 1);

INSERT INTO Translations (NarrationID, LangCode, TranslatedContent) VALUES
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'cho-noi-nga-nam'), 'en', N'Ngã Năm Floating Market gathers where five waterways meet, with boats bringing local produce and goods to trade. Visit early in the morning to experience the river life and trading traditions of the Mekong Delta.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'cho-noi-nga-nam'), 'fr', N'Le marché flottant de Ngã Năm se tient au confluent de cinq voies d''eau, où les bateaux viennent échanger produits et marchandises. Venez tôt le matin pour découvrir la vie fluviale du delta du Mékong.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-doi'), 'en', N'Chùa Dơi, also known as Mahatup Pagoda, is a well-known Khmer Theravada Buddhist temple in Sóc Trăng. Its Khmer architecture and leafy grounds are home to colonies of fruit bats roosting in the trees.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-doi'), 'fr', N'La pagode des Chauves-souris, également appelée Mahatup, est un temple bouddhiste theravāda khmer réputé de Sóc Trăng. Son architecture khmère et ses jardins ombragés accueillent des colonies de chauves-souris.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-la-han'), 'en', N'Chùa La Hán is a temple closely associated with Sóc Trăng''s Chinese community. Discover its colorful architectural details, serene worship spaces and the cultural influences that shape this region.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-la-han'), 'fr', N'La pagode La Hán est étroitement liée à la communauté chinoise de Sóc Trăng. Découvrez ses détails architecturaux colorés, ses espaces de culte paisibles et les influences culturelles de la région.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-phat-hoc-2'), 'en', N'Chùa Phật Học 2 is a popular spiritual destination in Sóc Trăng city. Take time to explore its spacious grounds, view the Buddhist structures and enjoy a quiet pause during your visit.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-phat-hoc-2'), 'fr', N'La pagode Phật Học 2 est un lieu spirituel très visité dans la ville de Sóc Trăng. Prenez le temps de parcourir son vaste espace, d''admirer ses édifices bouddhistes et de profiter d''un moment de calme.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'khu-can-cu-tinh-uy-soc-trang'), 'en', N'The Sóc Trăng Provincial Party Committee Base in Mỹ Phước Forest is associated with the local revolutionary movement during the resistance wars. A visit offers insight into the region''s history and life at the wartime base.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'khu-can-cu-tinh-uy-soc-trang'), 'fr', N'La base du Comité provincial du Parti de Sóc Trăng, dans la forêt de Mỹ Phước, est liée au mouvement révolutionnaire local durant les guerres de résistance. La visite permet de découvrir l''histoire de la région et la vie dans cette base.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'tan-hue-vien'), 'en', N'Tân Huê Viên is a popular stop associated with Sóc Trăng''s local specialties, especially bánh pía. Discover this flaky pastry traditionally filled with mung bean, durian and salted egg yolk, a familiar taste of the Mekong Delta.'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'tan-hue-vien'), 'fr', N'Tân Huê Viên est une étape réputée pour les spécialités de Sóc Trăng, notamment le bánh pía. Découvrez cette pâtisserie feuilletée traditionnellement garnie de haricot mungo, de durian et de jaune d''œuf salé.');

INSERT INTO Audio_Files (NarrationID, LangCode, VoiceName, SourceType, AudioURL) VALUES
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'cho-noi-nga-nam'), 'vi', N'vi-VN-Standard', 'TTS', N'nganam_vi.mp3'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-la-han'), 'vi', N'vi-VN-Standard', 'TTS', N'chualahan_vi.mp3'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'chua-phat-hoc-2'), 'vi', N'vi-VN-Standard', 'TTS', N'chuaphathoc2_vi.mp3'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'khu-can-cu-tinh-uy-soc-trang'), 'vi', N'vi-VN-Standard', 'TTS', N'khucancu_vi.mp3'),
((SELECT n.NarrationID FROM Narrations n JOIN POIs p ON p.PoiID = n.PoiID WHERE p.Slug = N'tan-hue-vien'), 'vi', N'vi-VN-Standard', 'TTS', N'tanhuevien_vi.mp3');
UPDATE POIs SET DurationMinutes = 7, Rating = 4.8, ReviewCount = 320, ImageSourceUrl = N'https://www.dacsanbanhpia.com/wp-content/uploads/2015/05/banh-pia-tan-hue-vien.jpg', ImagePosition = N'center 55%', HighlightsJson = N'["Đặc sản Sóc Trăng", "Bánh pía", "Ẩm thực địa phương"]', GalleryJson = N'[{"url": "https://upload.wikimedia.org/wikipedia/commons/3/30/Khu_B%E1%BA%A3o_Th%C3%A1p_Li%C3%AAn_Hoa_-_T%C3%A2n_Hu%C3%AA_Vi%C3%AAn.jpg", "sourceUrl": "https://commons.wikimedia.org/wiki/File:Khu_B%E1%BA%A3o_Th%C3%A1p_Li%C3%AAn_Hoa_-_T%C3%A2n_Hu%C3%AA_Vi%C3%AAn.jpg", "caption": "Khu Bảo Tháp Liên Hoa"}]' WHERE Slug = N'tan-hue-vien';
UPDATE POIs SET DurationMinutes = 8, Rating = 4.8, ReviewCount = 280, ImageSourceUrl = N'https://images.vietnamtourism.gov.vn/vn/images/2014/AnhInternet/chonoinga5.jpg', ImagePosition = N'center 50%', HighlightsJson = N'["Chợ nổi miền Tây", "Giao thương đường thủy", "Bình minh"]', GalleryJson = N'[{"url": "https://upload.wikimedia.org/wikipedia/commons/3/3e/Ch%E1%BB%A3_n%E1%BB%95i_Ng%C3%A3_N%C4%83m%2C_S%C3%B3c_Tr%C4%83ng.jpg", "sourceUrl": "https://commons.wikimedia.org/wiki/File:Ch%E1%BB%A3_n%E1%BB%95i_Ng%C3%A3_N%C4%83m,_S%C3%B3c_Tr%C4%83ng.jpg", "caption": "Ghe thuyền tại chợ nổi Ngã Năm"}]' WHERE Slug = N'cho-noi-nga-nam';
UPDATE POIs SET DurationMinutes = 8, Rating = 4.8, ReviewCount = 410, ImageSourceUrl = N'https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_D%C6%A1i_S%C3%B3c_Tr%C4%83ng_(40553739771).jpg', ImagePosition = N'center 55%', HighlightsJson = N'["Chùa Khmer", "Kiến trúc Phật giáo", "Vườn cây"]', GalleryJson = N'[{"url": "https://upload.wikimedia.org/wikipedia/commons/thumb/0/00/Ch%C3%A1nh_%C4%91i%E1%BB%87n_ch%C3%B9a_D%C6%A1i_-_S%C3%B3c_Tr%C4%83ng.jpg/960px-Ch%C3%A1nh_%C4%91i%E1%BB%87n_ch%C3%B9a_D%C6%A1i_-_S%C3%B3c_Tr%C4%83ng.jpg", "sourceUrl": "https://commons.wikimedia.org/wiki/File:Ch%C3%A1nh_%C4%91i%E1%BB%87n_ch%C3%B9a_D%C6%A1i_-_S%C3%B3c_Tr%C4%83ng.jpg", "caption": "Chánh điện chùa Dơi"}, {"url": "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ec/Ch%C3%B9a_D%C6%A1i%2C_c%E1%BB%95ng_ch%C3%ADnh.JPG/960px-Ch%C3%B9a_D%C6%A1i%2C_c%E1%BB%95ng_ch%C3%ADnh.JPG", "sourceUrl": "https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_D%C6%A1i,_c%E1%BB%95ng_ch%C3%ADnh.JPG", "caption": "Cổng chính chùa Dơi"}]' WHERE Slug = N'chua-doi';
UPDATE POIs SET DurationMinutes = 8, Rating = 4.8, ReviewCount = 365, ImageSourceUrl = N'https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_Ph%E1%BA%ADt_h%E1%BB%8Dc_2_-_S%C3%B3c_Tr%C4%83ng.jpg', ImagePosition = N'center 50%', HighlightsJson = N'["Không gian thanh tịnh", "Kiến trúc Phật giáo", "Vườn tượng"]', GalleryJson = N'[]' WHERE Slug = N'chua-phat-hoc-2';
UPDATE POIs SET DurationMinutes = 7, Rating = 4.7, ReviewCount = 230, ImageSourceUrl = N'https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg', ImagePosition = N'center 58%', HighlightsJson = N'["Văn hóa người Hoa", "Kiến trúc chùa", "Không gian thanh tịnh"]', GalleryJson = N'[{"url": "https://upload.wikimedia.org/wikipedia/commons/7/78/Ch%C3%B9a_La_H%C3%A1n_-_TP_S%C3%B3c_Tr%C4%83ng.jpg", "sourceUrl": "https://commons.wikimedia.org/wiki/File:Ch%C3%B9a_La_H%C3%A1n_-_TP_S%C3%B3c_Tr%C4%83ng.jpg", "caption": "Toàn cảnh chùa La Hán"}, {"url": "https://upload.wikimedia.org/wikipedia/commons/5/52/H%E1%BB%93_sen_ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg", "sourceUrl": "https://commons.wikimedia.org/wiki/File:H%E1%BB%93_sen_ch%C3%B9a_La_H%C3%A1n_-_S%C3%B3c_Tr%C4%83ng.jpg", "caption": "Hồ sen chùa La Hán"}]' WHERE Slug = N'chua-la-han';
UPDATE POIs SET DurationMinutes = 10, Rating = 4.7, ReviewCount = 190, ImageSourceUrl = N'https://media-cdn-v2.laodong.vn/storage/newsportal/2025/4/20/1494178/Soc-Trang-1-01.jpg', ImagePosition = N'center 50%', HighlightsJson = N'["Di tích lịch sử", "Căn cứ cách mạng", "Rừng Mỹ Phước"]', GalleryJson = N'[]' WHERE Slug = N'khu-can-cu-tinh-uy-soc-trang';
GO

SELECT * FROM Admin_Accounts;
SELECT * FROM Languages;
SELECT * FROM POIs;
SELECT * FROM Narrations;
SELECT * FROM Translations;
SELECT * FROM Audio_Files;
GO
