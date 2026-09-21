CREATE TABLE [Roles] (
  [RoleID] int PRIMARY KEY,
  [RoleName] nvarchar
)
GO

CREATE TABLE [Users] (
  [UserID] int PRIMARY KEY,
  [RoleID] int,
  [FullName] nvarchar,
  [Email] nvarchar(255),
  [PasswordHash] nvarchar(255)
)
GO

CREATE TABLE [POIs] (
  [PoiID] int PRIMARY KEY,
  [UserID] int,
  [OriginalName] nvarchar,
  [OriginalDescription] nvarchar,
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
  [LangCode] nvarchar(255) PRIMARY KEY,
  [LangName] nvarchar
)
GO

CREATE TABLE [POI_Localizations] (
  [LocalizationID] int PRIMARY KEY,
  [PoiID] int,
  [LangCode] nvarchar(255),
  [TranslatedName] nvarchar,
  [TranslatedDescription] nvarchar,
  [AudioURL] nvarchar(255)
)
GO

CREATE TABLE [Audio_Histories] (
  [HistoryID] int PRIMARY KEY,
  [PoiID] int,
  [LangCode] nvarchar(255),
  [CreatedDate] datetime,
  [Status] nvarchar(255)
)
GO

ALTER TABLE [Users] ADD FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID])
GO

ALTER TABLE [POIs] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [POI_Images] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
GO

ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
GO

ALTER TABLE [POI_Localizations] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO

ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
GO

ALTER TABLE [Audio_Histories] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO
