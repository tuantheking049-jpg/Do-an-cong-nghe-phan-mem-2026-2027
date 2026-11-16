using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using VisitorGuide.Infrastructure.Entities;

namespace VisitorGuide.Infrastructure;

public sealed class SqlServerRelationalStore(string connectionString)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<DiaDiemEntity>> LoadPlacesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var places = new Dictionary<string, DiaDiemEntity>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Slug, Name, Category, Province, DurationMinutes, Rating, ReviewCount,
                       ImageUrl, ImageSourceUrl, ImagePosition, Description, HighlightsJson, GalleryJson
                FROM dbo.POIs
                ORDER BY DisplayOrder, PoiID;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var place = new DiaDiemEntity
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Category = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Province = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    DurationMinutes = reader.GetInt32(4),
                    Rating = reader.GetDouble(5),
                    ReviewCount = reader.GetInt32(6),
                    ImageUrl = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    ImageSourceUrl = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    ImagePosition = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Highlights = ReadHighlights(reader.IsDBNull(11) ? null : reader.GetString(11)),
                    GalleryPhotos = ReadPhotos(reader.IsDBNull(12) ? null : reader.GetString(12))
                };
                var legacyDescription = reader.IsDBNull(10) ? "" : reader.GetString(10);
                if (!string.IsNullOrWhiteSpace(legacyDescription))
                {
                    place.Descriptions.Add(new DiaDiemMoTaEntity
                    {
                        DiaDiemId = place.Id,
                        LanguageCode = "vi",
                        Description = legacyDescription
                    });
                }

                places.Add(place.Id, place);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.Slug, n.SourceLang, n.Content
                FROM dbo.Narrations n
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE n.Content IS NOT NULL;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var placeId = reader.GetString(0);
                if (!places.TryGetValue(placeId, out var place)) continue;

                var languageCode = reader.GetString(1);
                var description = reader.GetString(2);
                SetDescription(place, languageCode, description);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.Slug, t.LangCode, t.TranslatedContent
                FROM dbo.Translations t
                JOIN dbo.Narrations n ON n.NarrationID = t.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var placeId = reader.GetString(0);
                if (!places.TryGetValue(placeId, out var place)) continue;
                SetDescription(place, reader.GetString(1), reader.GetString(2));
            }
        }

        foreach (var place in places.Values)
        {
            if (place.Descriptions.All(description =>
                    !description.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase)))
            {
                place.Descriptions.Add(new DiaDiemMoTaEntity
                {
                    DiaDiemId = place.Id,
                    LanguageCode = "vi",
                    Description = ""
                });
            }
        }

        return places.Values.ToList();
    }

    public async Task SavePlacesAsync(
        IReadOnlyCollection<DiaDiemEntity> places,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var adminId = await GetAdminIdAsync(connection, transaction, cancellationToken);
            await EnsureLanguagesAsync(connection, transaction, places, cancellationToken);

            var incomingIds = new HashSet<string>(
                places.Select(place => place.Id),
                StringComparer.OrdinalIgnoreCase);
            var existingIds = await ReadPlaceIdsAsync(connection, transaction, cancellationToken);
            foreach (var existingId in existingIds.Where(id => !incomingIds.Contains(id)))
            {
                await using var deletePlace = connection.CreateCommand();
                deletePlace.Transaction = transaction;
                deletePlace.CommandText = "DELETE FROM dbo.POIs WHERE Slug = @Slug;";
                deletePlace.Parameters.AddWithValue("@Slug", existingId);
                await deletePlace.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var place in places)
            {
                var descriptions = place.Descriptions
                    .GroupBy(item => item.LanguageCode, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .ToList();
                var vietnameseText = descriptions
                    .FirstOrDefault(item => item.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))
                    ?.Description ?? "";
                var highlightsJson = JsonSerializer.Serialize(
                    place.Highlights.OrderBy(item => item.Position).Select(item => item.Text).ToArray(),
                    JsonOptions);
                var galleryJson = JsonSerializer.Serialize(
                    place.GalleryPhotos.OrderBy(item => item.Position)
                        .Select(item => new PhotoData(item.Url, item.SourceUrl, item.Caption))
                        .ToArray(),
                    JsonOptions);

                await UpsertPlaceAsync(
                    connection,
                    transaction,
                    place,
                    vietnameseText,
                    highlightsJson,
                    galleryJson,
                    adminId,
                    cancellationToken);
                var narrationId = await UpsertNarrationAsync(
                    connection,
                    transaction,
                    place,
                    vietnameseText,
                    adminId,
                    cancellationToken);

                var translationDescriptions = descriptions
                    .Where(item => !item.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase)
                        && !IsEmptyTranslation(item.Description))
                    .ToDictionary(item => item.LanguageCode.Trim(), item => item.Description, StringComparer.OrdinalIgnoreCase);
                var existingLanguages = await ReadTranslationLanguagesAsync(
                    connection,
                    transaction,
                    narrationId,
                    cancellationToken);

                foreach (var languageCode in existingLanguages.Where(code => !translationDescriptions.ContainsKey(code)))
                {
                    await DeleteTranslationAsync(
                        connection,
                        transaction,
                        narrationId,
                        languageCode,
                        cancellationToken);
                }

                foreach (var (languageCode, content) in translationDescriptions)
                {
                    await UpsertTranslationAsync(
                        connection,
                        transaction,
                        narrationId,
                        languageCode,
                        content,
                        cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<string> LoadAdminStateJsonAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var state = new AdminStateData();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Slug, Status, DisplayOrder, PlayCount
                FROM dbo.POIs;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetString(0);
                state.OrderById[id] = reader.GetInt32(2);
                state.StatusById[id] = reader.GetString(1);
                state.PlaysById[id] = reader.GetInt32(3);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT LangCode, LangName, IsEnabled, VoiceLocale, DisplayOrder
                FROM dbo.Languages
                ORDER BY DisplayOrder, LangCode;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var code = reader.GetString(0).Trim().ToLowerInvariant();
                state.Languages.Add(new LanguageData
                {
                    Code = code,
                    Name = reader.GetString(1),
                    VoiceLocale = reader.IsDBNull(3) || string.IsNullOrWhiteSpace(reader.GetString(3))
                        ? GetDefaultVoiceLocale(code)
                        : reader.GetString(3),
                    Status = reader.GetBoolean(2) ? "Đang hỗ trợ" : "Tạm ngưng",
                    OrderIndex = reader.GetInt32(4)
                });
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.Slug, n.Title, n.PublishStatus
                FROM dbo.Narrations n
                JOIN dbo.POIs p ON p.PoiID = n.PoiID;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                state.NarrationTitleById[reader.GetString(0)] = reader.GetString(1);
                state.NarrationPublishStatusById[reader.GetString(0)] = reader.GetString(2);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.Slug, t.LangCode, t.Status, t.DisplayOrder
                FROM dbo.Translations t
                JOIN dbo.Narrations n ON n.NarrationID = t.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = GetKey(reader.GetString(0), reader.GetString(1));
                state.TranslationStatusByKey[key] = reader.GetString(2);
                state.TranslationOrderByKey[key] = reader.GetInt32(3);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.Slug, a.LangCode, a.SourceType, a.VoiceName, a.SpeechRate,
                       a.Pitch, a.AudioURL, a.Status, a.DisplayOrder
                FROM dbo.Audio_Files a
                JOIN dbo.Narrations n ON n.NarrationID = a.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE a.LangCode = n.SourceLang
                   OR EXISTS (
                       SELECT 1 FROM dbo.Translations t
                       WHERE t.NarrationID = a.NarrationID AND t.LangCode = a.LangCode
                   );
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var placeId = reader.GetString(0);
                var languageCode = reader.GetString(1).Trim().ToLowerInvariant();
                var key = GetKey(placeId, languageCode);
                state.Voices.Add(new VoiceData
                {
                    Key = key,
                    DiaDiemId = placeId,
                    LanguageCode = languageCode,
                    SourceType = reader.GetString(2),
                    VoiceName = reader.IsDBNull(3) ? GetDefaultVoiceName(languageCode) : reader.GetString(3),
                    SpeechRate = reader.GetDouble(4),
                    Pitch = reader.GetDouble(5),
                    AudioUrl = reader.GetString(6),
                    Status = reader.GetString(7),
                    OrderIndex = reader.GetInt32(8)
                });
            }
        }

        if (state.Languages.All(language => !language.Code.Equals("vi", StringComparison.OrdinalIgnoreCase)))
        {
            state.Languages.Insert(0, new LanguageData
            {
                Code = "vi",
                Name = "Tiếng Việt",
                VoiceLocale = "vi-VN",
                Status = "Đang hỗ trợ",
                OrderIndex = 1
            });
        }

        state.NextOrder = Math.Max(7, state.OrderById.Values.DefaultIfEmpty(0).Max() + 1);
        state.NextLangOrder = Math.Max(1, state.Languages.Select(language => language.OrderIndex).DefaultIfEmpty(0).Max() + 1);
        state.NextTranslationOrder = Math.Max(1, state.TranslationOrderByKey.Values.DefaultIfEmpty(0).Max() + 1);
        state.NextVoiceOrder = Math.Max(1, state.Voices.Select(voice => voice.OrderIndex).DefaultIfEmpty(0).Max() + 1);
        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task SaveAdminStateAsync(
        string stateJson,
        bool deleteMissingVoices = true,
        bool syncPlayCounts = false,
        CancellationToken cancellationToken = default)
    {
        var state = JsonSerializer.Deserialize<AdminStateData>(
            stateJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Admin state could not be read for SQL persistence.");
        EnsureVietnameseLanguage(state);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await SyncLanguagesAsync(connection, transaction, state.Languages, cancellationToken);
            await SyncPlaceStateAsync(connection, transaction, state, syncPlayCounts, cancellationToken);
            await SyncNarrationStateAsync(connection, transaction, state, cancellationToken);
            await SyncTranslationStateAsync(connection, transaction, state, cancellationToken);
            await SyncVoicesAsync(connection, transaction, state.Voices, deleteMissingVoices, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task EnsureDefaultAudioFilesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var items = new List<(int NarrationId, string LanguageCode, string VoiceName)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT n.NarrationID, n.SourceLang, p.Slug
                FROM dbo.Narrations n
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE NULLIF(LTRIM(RTRIM(n.Content)), N'') IS NOT NULL
                  AND n.Content NOT IN (N'(Chưa có nội dung thuyết minh)', N'(Chưa có bản dịch)')
                UNION
                SELECT n.NarrationID, t.LangCode, p.Slug
                FROM dbo.Translations t
                JOIN dbo.Narrations n ON n.NarrationID = t.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE NULLIF(LTRIM(RTRIM(t.TranslatedContent)), N'') IS NOT NULL
                  AND t.TranslatedContent <> N'(Chưa có bản dịch)';
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var langCode = reader.GetString(1).Trim().ToLowerInvariant();
                items.Add((reader.GetInt32(0), langCode, GetDefaultVoiceName(langCode)));
            }
        }

        foreach (var item in items)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Audio_Files
                    WHERE NarrationID = @NarrationID AND LangCode = @LangCode
                )
                BEGIN
                    INSERT INTO dbo.Audio_Files
                        (NarrationID, LangCode, VoiceName, SourceType, AudioURL,
                         SpeechRate, Pitch, Status, DisplayOrder)
                    VALUES
                        (@NarrationID, @LangCode, @VoiceName, N'TTS', N'',
                         1.0, 1.0, N'Đã công bố',
                         ISNULL((SELECT MAX(DisplayOrder) + 1 FROM dbo.Audio_Files), 1));
                END;
                """;
            command.Parameters.AddWithValue("@NarrationID", item.NarrationId);
            command.Parameters.AddWithValue("@LangCode", item.LanguageCode);
            command.Parameters.AddWithValue("@VoiceName", item.VoiceName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<int?> IncrementPlayCountAsync(string placeId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.POIs
            SET PlayCount = PlayCount + 1
            OUTPUT inserted.PlayCount
            WHERE Slug = @Slug;
            """;
        command.Parameters.AddWithValue("@Slug", placeId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int count ? count : null;
    }

    private static async Task UpsertPlaceAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DiaDiemEntity place,
        string description,
        string highlightsJson,
        string galleryJson,
        int adminId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM dbo.POIs WHERE Slug = @Slug)
            BEGIN
                UPDATE dbo.POIs
                SET Name = @Name, Description = @Description, Category = @Category,
                    Province = @Province, ImageUrl = @ImageUrl,
                    DurationMinutes = @DurationMinutes, Rating = @Rating,
                    ReviewCount = @ReviewCount, ImageSourceUrl = @ImageSourceUrl,
                    ImagePosition = @ImagePosition, HighlightsJson = @HighlightsJson,
                    GalleryJson = @GalleryJson
                WHERE Slug = @Slug;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.POIs
                    (Slug, Name, Description, Category, Province, ImageUrl,
                     DurationMinutes, Rating, ReviewCount, ImageSourceUrl, ImagePosition,
                     HighlightsJson, GalleryJson, CreatedBy)
                VALUES
                    (@Slug, @Name, @Description, @Category, @Province, @ImageUrl,
                     @DurationMinutes, @Rating, @ReviewCount, @ImageSourceUrl, @ImagePosition,
                     @HighlightsJson, @GalleryJson, @AdminID);
            END;
            """;
        command.Parameters.AddWithValue("@Slug", place.Id);
        command.Parameters.AddWithValue("@Name", place.Name);
        command.Parameters.AddWithValue("@Description", description);
        command.Parameters.AddWithValue("@Category", place.Category);
        command.Parameters.AddWithValue("@Province", place.Province);
        command.Parameters.AddWithValue("@ImageUrl", place.ImageUrl);
        command.Parameters.AddWithValue("@DurationMinutes", place.DurationMinutes);
        command.Parameters.AddWithValue("@Rating", place.Rating);
        command.Parameters.AddWithValue("@ReviewCount", place.ReviewCount);
        command.Parameters.AddWithValue("@ImageSourceUrl", place.ImageSourceUrl);
        command.Parameters.AddWithValue("@ImagePosition", place.ImagePosition);
        command.Parameters.AddWithValue("@HighlightsJson", highlightsJson);
        command.Parameters.AddWithValue("@GalleryJson", galleryJson);
        command.Parameters.AddWithValue("@AdminID", adminId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> UpsertNarrationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DiaDiemEntity place,
        string content,
        int adminId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.Narrations
            SET Content = @Content, SourceLang = N'vi', UpdatedAt = GETDATE()
            WHERE PoiID = (SELECT PoiID FROM dbo.POIs WHERE Slug = @Slug);
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO dbo.Narrations
                    (PoiID, SourceLang, Title, Content, PublishStatus, UpdatedBy)
                SELECT PoiID, N'vi', @Title, @Content, N'Bản nháp', @AdminID
                FROM dbo.POIs WHERE Slug = @Slug;
            END;
            SELECT NarrationID
            FROM dbo.Narrations
            WHERE PoiID = (SELECT PoiID FROM dbo.POIs WHERE Slug = @Slug);
            """;
        command.Parameters.AddWithValue("@Slug", place.Id);
        command.Parameters.AddWithValue("@Title", $"Thuyết minh {place.Name}");
        command.Parameters.AddWithValue("@Content", content);
        command.Parameters.AddWithValue("@AdminID", adminId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int narrationId
            ? narrationId
            : throw new InvalidOperationException($"Could not create narration for place '{place.Id}'.");
    }

    private static async Task UpsertTranslationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int narrationId,
        string languageCode,
        string content,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.Translations
            SET TranslatedContent = @Content, UpdatedAt = GETDATE()
            WHERE NarrationID = @NarrationID AND LangCode = @LangCode;
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO dbo.Translations
                    (NarrationID, LangCode, TranslatedContent)
                VALUES (@NarrationID, @LangCode, @Content);
            END;
            """;
        command.Parameters.AddWithValue("@NarrationID", narrationId);
        command.Parameters.AddWithValue("@LangCode", languageCode);
        command.Parameters.AddWithValue("@Content", content);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteTranslationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int narrationId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM dbo.Audio_Files
            WHERE NarrationID = @NarrationID AND LangCode = @LangCode;
            DELETE FROM dbo.Translations
            WHERE NarrationID = @NarrationID AND LangCode = @LangCode;
            """;
        command.Parameters.AddWithValue("@NarrationID", narrationId);
        command.Parameters.AddWithValue("@LangCode", languageCode);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureLanguagesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyCollection<DiaDiemEntity> places,
        CancellationToken cancellationToken)
    {
        var languages = places.SelectMany(place => place.Descriptions)
            .Select(description => description.LanguageCode.Trim().ToLowerInvariant())
            .Append("vi")
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var language in languages)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE LangCode = @LangCode)
                BEGIN
                    INSERT INTO dbo.Languages (LangCode, LangName, IsEnabled, VoiceLocale, DisplayOrder)
                    VALUES (@LangCode, @LangName, 1, @VoiceLocale,
                        ISNULL((SELECT MAX(DisplayOrder) + 1 FROM dbo.Languages), 1));
                END;
                """;
            command.Parameters.AddWithValue("@LangCode", language);
            command.Parameters.AddWithValue("@LangName", language);
            command.Parameters.AddWithValue("@VoiceLocale", GetDefaultVoiceLocale(language));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task SyncLanguagesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        List<LanguageData> languages,
        CancellationToken cancellationToken)
    {
        EnsureVietnameseLanguage(new AdminStateData { Languages = languages });
        var desired = languages.ToDictionary(
            language => language.Code.Trim().ToLowerInvariant(),
            StringComparer.OrdinalIgnoreCase);
        var existing = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT LangCode FROM dbo.Languages;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) existing.Add(reader.GetString(0));
        }

        foreach (var code in existing.Where(code => !desired.ContainsKey(code)))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                DELETE FROM dbo.Audio_Files WHERE LangCode = @LangCode;
                DELETE FROM dbo.Translations WHERE LangCode = @LangCode;
                DELETE FROM dbo.Languages WHERE LangCode = @LangCode;
                """;
            command.Parameters.AddWithValue("@LangCode", code);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var language in desired.Values)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE dbo.Languages
                SET LangName = @Name, IsEnabled = @IsEnabled,
                    VoiceLocale = @VoiceLocale, DisplayOrder = @DisplayOrder
                WHERE LangCode = @LangCode;
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO dbo.Languages
                        (LangCode, LangName, IsEnabled, VoiceLocale, DisplayOrder)
                    VALUES
                        (@LangCode, @Name, @IsEnabled, @VoiceLocale, @DisplayOrder);
                END;
                """;
            command.Parameters.AddWithValue("@LangCode", language.Code.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@Name", language.Name);
            command.Parameters.AddWithValue("@IsEnabled", !language.Status.Equals("Tạm ngưng", StringComparison.OrdinalIgnoreCase));
            command.Parameters.AddWithValue("@VoiceLocale", language.VoiceLocale);
            command.Parameters.AddWithValue("@DisplayOrder", language.OrderIndex);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task SyncPlaceStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        AdminStateData state,
        bool syncPlayCounts,
        CancellationToken cancellationToken)
    {
        foreach (var (placeId, status) in state.StatusById)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE dbo.POIs SET Status = @Status WHERE Slug = @Slug;";
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@Slug", placeId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var (placeId, order) in state.OrderById)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE dbo.POIs SET DisplayOrder = @DisplayOrder WHERE Slug = @Slug;";
            command.Parameters.AddWithValue("@DisplayOrder", order);
            command.Parameters.AddWithValue("@Slug", placeId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!syncPlayCounts) return;

        foreach (var (placeId, plays) in state.PlaysById)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE dbo.POIs SET PlayCount = @PlayCount WHERE Slug = @Slug;";
            command.Parameters.AddWithValue("@PlayCount", plays);
            command.Parameters.AddWithValue("@Slug", placeId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task SyncNarrationStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        AdminStateData state,
        CancellationToken cancellationToken)
    {
        var placeIds = state.NarrationTitleById.Keys
            .Union(state.NarrationPublishStatusById.Keys, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var placeId in placeIds)
        {
            state.NarrationTitleById.TryGetValue(placeId, out var title);
            state.NarrationPublishStatusById.TryGetValue(placeId, out var status);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE n
                SET Title = COALESCE(NULLIF(@Title, N''), n.Title),
                    PublishStatus = COALESCE(NULLIF(@Status, N''), n.PublishStatus),
                    UpdatedAt = GETDATE()
                FROM dbo.Narrations n
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE p.Slug = @Slug;
                """;
            command.Parameters.AddWithValue("@Title", title ?? "");
            command.Parameters.AddWithValue("@Status", status ?? "");
            command.Parameters.AddWithValue("@Slug", placeId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task SyncTranslationStateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        AdminStateData state,
        CancellationToken cancellationToken)
    {
        foreach (var (key, status) in state.TranslationStatusByKey)
        {
            if (!TryParseKey(key, out var placeId, out var languageCode)) continue;
            state.TranslationOrderByKey.TryGetValue(key, out var order);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE t
                SET Status = @Status, DisplayOrder = @DisplayOrder
                FROM dbo.Translations t
                JOIN dbo.Narrations n ON n.NarrationID = t.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE p.Slug = @Slug AND t.LangCode = @LangCode;
                """;
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@DisplayOrder", order);
            command.Parameters.AddWithValue("@Slug", placeId);
            command.Parameters.AddWithValue("@LangCode", languageCode);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task SyncVoicesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        List<VoiceData> voices,
        bool deleteMissing,
        CancellationToken cancellationToken)
    {
        var desiredKeys = voices
            .Select(voice => GetKey(voice.DiaDiemId, voice.LanguageCode))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (deleteMissing)
        {
            var existing = new List<(int NarrationId, string PlaceId, string LanguageCode)>();
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    SELECT a.NarrationID, p.Slug, a.LangCode
                    FROM dbo.Audio_Files a
                    JOIN dbo.Narrations n ON n.NarrationID = a.NarrationID
                    JOIN dbo.POIs p ON p.PoiID = n.PoiID;
                    """;
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    existing.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
                }
            }

            foreach (var item in existing.Where(item =>
                         !desiredKeys.Contains(GetKey(item.PlaceId, item.LanguageCode))))
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    DELETE FROM dbo.Audio_Files
                    WHERE NarrationID = @NarrationID AND LangCode = @LangCode;
                    """;
                command.Parameters.AddWithValue("@NarrationID", item.NarrationId);
                command.Parameters.AddWithValue("@LangCode", item.LanguageCode);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        foreach (var voice in voices)
        {
            if (!TryParseKey(voice.Key, out var placeId, out var languageCode))
            {
                placeId = voice.DiaDiemId;
                languageCode = voice.LanguageCode;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE a
                SET VoiceName = @VoiceName, SourceType = @SourceType, AudioURL = @AudioURL,
                    SpeechRate = @SpeechRate, Pitch = @Pitch, Status = @Status,
                    DisplayOrder = @DisplayOrder
                FROM dbo.Audio_Files a
                JOIN dbo.Narrations n ON n.NarrationID = a.NarrationID
                JOIN dbo.POIs p ON p.PoiID = n.PoiID
                WHERE p.Slug = @Slug AND a.LangCode = @LangCode;
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO dbo.Audio_Files
                        (NarrationID, LangCode, VoiceName, SourceType, AudioURL,
                         SpeechRate, Pitch, Status, DisplayOrder)
                    SELECT n.NarrationID, @LangCode, @VoiceName, @SourceType, @AudioURL,
                           @SpeechRate, @Pitch, @Status, @DisplayOrder
                    FROM dbo.Narrations n
                    JOIN dbo.POIs p ON p.PoiID = n.PoiID
                    WHERE p.Slug = @Slug;
                END;
                """;
            command.Parameters.AddWithValue("@VoiceName", voice.VoiceName);
            command.Parameters.AddWithValue("@SourceType", voice.SourceType);
            command.Parameters.AddWithValue("@AudioURL", voice.AudioUrl);
            command.Parameters.AddWithValue("@SpeechRate", voice.SpeechRate);
            command.Parameters.AddWithValue("@Pitch", voice.Pitch);
            command.Parameters.AddWithValue("@Status", voice.Status);
            command.Parameters.AddWithValue("@DisplayOrder", voice.OrderIndex);
            command.Parameters.AddWithValue("@Slug", placeId);
            command.Parameters.AddWithValue("@LangCode", languageCode);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<int> GetAdminIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT TOP (1) AdminID FROM dbo.Admin_Accounts ORDER BY AdminID;";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int adminId
            ? adminId
            : throw new InvalidOperationException("dbo.Admin_Accounts must contain an administrator account.");
    }

    private static async Task<List<string>> ReadPlaceIdsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var ids = new List<string>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Slug FROM dbo.POIs;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetString(0));
        return ids;
    }

    private static async Task<List<string>> ReadTranslationLanguagesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int narrationId,
        CancellationToken cancellationToken)
    {
        var languages = new List<string>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT LangCode FROM dbo.Translations WHERE NarrationID = @NarrationID;";
        command.Parameters.AddWithValue("@NarrationID", narrationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) languages.Add(reader.GetString(0));
        return languages;
    }

    private static List<DiaDiemDiemNoiBatEntity> ReadHighlights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        var values = JsonSerializer.Deserialize<string[]>(json, JsonOptions)
            ?? throw new InvalidDataException("POIs.HighlightsJson contains invalid JSON.");
        return values.Select((text, index) => new DiaDiemDiemNoiBatEntity
        {
            Position = index,
            Text = text
        }).ToList();
    }

    private static List<DiaDiemAnhEntity> ReadPhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        var values = JsonSerializer.Deserialize<List<PhotoData>>(json, JsonOptions)
            ?? throw new InvalidDataException("POIs.GalleryJson contains invalid JSON.");
        return values.Select((photo, index) => new DiaDiemAnhEntity
        {
            Position = index,
            Url = photo.Url,
            SourceUrl = photo.SourceUrl,
            Caption = photo.Caption
        }).ToList();
    }

    private static void SetDescription(DiaDiemEntity place, string languageCode, string description)
    {
        var existing = place.Descriptions.FirstOrDefault(item =>
            item.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            place.Descriptions.Add(new DiaDiemMoTaEntity
            {
                DiaDiemId = place.Id,
                LanguageCode = languageCode,
                Description = description
            });
        }
        else
        {
            existing.Description = description;
        }
    }

    private static bool IsEmptyTranslation(string content) =>
        string.IsNullOrWhiteSpace(content) ||
        content == "(Chưa có bản dịch)";

    private static void EnsureVietnameseLanguage(AdminStateData state)
    {
        if (state.Languages.Any(language =>
                language.Code.Equals("vi", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        state.Languages.Add(new LanguageData
        {
            Code = "vi",
            Name = "Tiếng Việt",
            VoiceLocale = "vi-VN",
            Status = "Đang hỗ trợ",
            OrderIndex = 1
        });
    }

    private static bool TryParseKey(string key, out string placeId, out string languageCode)
    {
        var separator = key.LastIndexOf(':');
        if (separator <= 0 || separator == key.Length - 1)
        {
            placeId = "";
            languageCode = "";
            return false;
        }

        placeId = key[..separator];
        languageCode = key[(separator + 1)..].ToLowerInvariant();
        return true;
    }

    private static string GetKey(string placeId, string languageCode) =>
        $"{placeId.Trim().ToLowerInvariant()}:{languageCode.Trim().ToLowerInvariant()}";

    private static string GetDefaultVoiceLocale(string code) => code.ToLowerInvariant() switch
    {
        "vi" => "vi-VN",
        "en" => "en-US",
        "fr" => "fr-FR",
        "ja" or "jp" => "ja-JP",
        "ko" or "kr" => "ko-KR",
        "zh" or "cn" => "zh-CN",
        _ => $"{code}-{code.ToUpperInvariant()}"
    };

    private static string GetDefaultVoiceName(string code) => code.ToLowerInvariant() switch
    {
        "vi" => "Google Tiếng Việt (vi-VN)",
        "en" => "Google US English (en-US)",
        "fr" => "Google Français (fr-FR)",
        "ja" or "jp" => "Google 日本語 (ja-JP)",
        "ko" => "Google 한국어 (ko-KR)",
        "zh" => "Google 普通话 (zh-CN)",
        _ => $"Google TTS ({code})"
    };

    private sealed record PhotoData(string Url, string SourceUrl, string Caption);

    private sealed class AdminStateData
    {
        public Dictionary<string, int> OrderById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> StatusById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> NarrationTitleById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> NarrationPublishStatusById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int NextOrder { get; set; } = 7;
        public List<LanguageData> Languages { get; set; } = [];
        public int NextLangOrder { get; set; } = 4;
        public Dictionary<string, string> TranslationStatusByKey { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> TranslationOrderByKey { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<VoiceData> Voices { get; set; } = [];
        public List<string> DeletedVoiceKeys { get; set; } = [];
        public Dictionary<string, int> PlaysById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int NextTranslationOrder { get; set; } = 1;
        public int NextVoiceOrder { get; set; } = 1;
    }

    private sealed class LanguageData
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string VoiceLocale { get; set; } = "";
        public string Status { get; set; } = "Đang hỗ trợ";
        public int OrderIndex { get; set; }
    }

    private sealed class VoiceData
    {
        public string Key { get; set; } = "";
        public string DiaDiemId { get; set; } = "";
        public string LanguageCode { get; set; } = "";
        public string SourceType { get; set; } = "TTS";
        public string VoiceName { get; set; } = "";
        public double SpeechRate { get; set; } = 1.0;
        public double Pitch { get; set; } = 1.0;
        public string AudioUrl { get; set; } = "";
        public string Status { get; set; } = "Đã công bố";
        public int OrderIndex { get; set; }
    }
}
