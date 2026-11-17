-- Sao luu DuLichSocTrang va places.json truoc khi chay.
-- Chi xoa bon bang ung dung; khong xoa sau bang loi trong database.sql.
USE DuLichSocTrang;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.DiemNoiBatDiaDiem', N'U') IS NOT NULL
    DROP TABLE dbo.DiemNoiBatDiaDiem;
IF OBJECT_ID(N'dbo.AnhBoSungDiaDiem', N'U') IS NOT NULL
    DROP TABLE dbo.AnhBoSungDiaDiem;
IF OBJECT_ID(N'dbo.MoTaDiaDiem', N'U') IS NOT NULL
    DROP TABLE dbo.MoTaDiaDiem;
IF OBJECT_ID(N'dbo.DiaDiemThamQuan', N'U') IS NOT NULL
    DROP TABLE dbo.DiaDiemThamQuan;

COMMIT TRANSACTION;
GO
