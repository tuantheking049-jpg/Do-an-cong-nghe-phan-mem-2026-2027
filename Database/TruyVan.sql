USE HuongDanDuLichQ4;
GO
-- 1.CÁC CÂU LỆNH CHO BẢNG NGƯỜI DÙNG (Users)
-- Thêm người dùng mới (Đăng ký)
INSERT INTO [Users] (UserID, RoleID, FullName, Email, PasswordHash) 
VALUES (3, 2, N'Trần Thị B', 'tranb@gmail.com', 'hash9999');
-- Sửa thông tin người dùng (Cập nhật profile)
UPDATE [Users] 
SET FullName = N'Trần Thị Bé Ba', Email = 'beba@gmail.com'
WHERE UserID = 3;
-- Tìm kiếm người dùng (Tìm theo tên hoặc lấy tất cả)
-- Lấy tất cả danh sách tài khoản:
SELECT * FROM [Users];
-- Tìm tài khoản có chữ "Trần":
SELECT * FROM [Users] WHERE FullName LIKE N'%Trần%';
-- Xóa người dùng 
DELETE FROM [Users] WHERE UserID = 3;


-- 2. CÁC CÂU LỆNH CHO BẢNG ĐỊA ĐIỂM (POIs)
-- Thêm địa điểm mới 
INSERT INTO [POIs] (PoiID, UserID, OriginalName, OriginalDescription, Latitude, Longitude) 
VALUES (2, 1, N'Cầu Mống', N'Cây cầu lịch sử nối Quận 1 và Quận 4', 10.7645, 106.7056);
-- Sửa thông tin địa điểm 
UPDATE [POIs] 
SET OriginalName = N'Cầu Mống Quận 4', OriginalDescription = N'Điểm check-in nổi tiếng'
WHERE PoiID = 2;
-- Tìm kiếm địa điểm 
-- Lấy tất cả địa điểm:
SELECT * FROM [POIs];
-- Tìm địa điểm có chữ "Cầu":
SELECT * FROM [POIs] WHERE OriginalName LIKE N'%Cầu%';
-- Xóa địa điểm
DELETE FROM [POIs] WHERE PoiID = 2;