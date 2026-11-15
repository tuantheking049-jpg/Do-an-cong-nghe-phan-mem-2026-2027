CREATE TABLE [Admin_Accounts] (
  [AdminID] int PRIMARY KEY,
  [Username] nvarchar(50),
  [FullName] nvarchar(100),
  [Email] nvarchar(255),
  [PasswordHash] nvarchar(255),
  [IsLocked] bit,
  [CreatedAt] datetime
)
GO

CREATE TABLE [Languages] (
  [LangCode] nvarchar(10) PRIMARY KEY,
  [LangName] nvarchar(100),
  [IsEnabled] bit
)
GO

CREATE TABLE [POIs] (
  [PoiID] int PRIMARY KEY,
  [Slug] nvarchar(100),
  [Name] nvarchar(255),
  [Description] nvarchar(max),
  [Category] nvarchar(100),
  [Province] nvarchar(150),
  [Address] nvarchar(255),
  [ImageUrl] nvarchar(2048),
  [Latitude] float,
  [Longitude] float,
  [Status] nvarchar(30),
  [DisplayOrder] int,
  [CreatedBy] int
)
GO

CREATE TABLE [Narrations] (
  [NarrationID] int PRIMARY KEY,
  [PoiID] int,
  [SourceLang] nvarchar(10),
  [Title] nvarchar(255),
  [Content] nvarchar(max),
  [PublishStatus] nvarchar(30),
  [UpdatedBy] int,
  [UpdatedAt] datetime
)
GO

CREATE TABLE [Translations] (
  [TranslationID] int PRIMARY KEY,
  [NarrationID] int,
  [LangCode] nvarchar(10),
  [TranslatedContent] nvarchar(max),
  [UpdatedAt] datetime
)
GO

CREATE TABLE [Audio_Files] (
  [AudioID] int PRIMARY KEY,
  [NarrationID] int,
  [LangCode] nvarchar(10),
  [VoiceName] nvarchar(100),
  [SourceType] nvarchar(10),
  [AudioURL] nvarchar(500),
  [CreatedAt] datetime
)
GO

ALTER TABLE [POIs] ADD FOREIGN KEY ([CreatedBy]) REFERENCES [Admin_Accounts] ([AdminID])
GO
ALTER TABLE [Narrations] ADD FOREIGN KEY ([PoiID]) REFERENCES [POIs] ([PoiID])
GO
ALTER TABLE [Narrations] ADD FOREIGN KEY ([SourceLang]) REFERENCES [Languages] ([LangCode])
GO
ALTER TABLE [Narrations] ADD FOREIGN KEY ([UpdatedBy]) REFERENCES [Admin_Accounts] ([AdminID])
GO
ALTER TABLE [Translations] ADD FOREIGN KEY ([NarrationID]) REFERENCES [Narrations] ([NarrationID])
GO
ALTER TABLE [Translations] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO
ALTER TABLE [Audio_Files] ADD FOREIGN KEY ([NarrationID]) REFERENCES [Narrations] ([NarrationID])
GO
ALTER TABLE [Audio_Files] ADD FOREIGN KEY ([LangCode]) REFERENCES [Languages] ([LangCode])
GO
