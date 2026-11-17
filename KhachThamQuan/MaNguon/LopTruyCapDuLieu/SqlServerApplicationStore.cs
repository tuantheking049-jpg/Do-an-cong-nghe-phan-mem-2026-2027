using System.Data;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace VisitorGuide.Infrastructure;

public sealed class SqlServerApplicationStore(string connectionString)
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.Admin_Accounts', N'U') IS NULL
                OR OBJECT_ID(N'dbo.Languages', N'U') IS NULL
                OR OBJECT_ID(N'dbo.POIs', N'U') IS NULL
                OR OBJECT_ID(N'dbo.Narrations', N'U') IS NULL
                OR OBJECT_ID(N'dbo.Translations', N'U') IS NULL
                OR OBJECT_ID(N'dbo.Audio_Files', N'U') IS NULL
                THROW 50001, 'One or more of the six required SQL tables are missing.', 1;
            IF COL_LENGTH(N'dbo.Admin_Accounts', N'ApplicationDataJson') IS NULL
                ALTER TABLE dbo.Admin_Accounts ADD ApplicationDataJson NVARCHAR(MAX) NULL;
            IF COL_LENGTH(N'dbo.Languages', N'VoiceLocale') IS NULL
                ALTER TABLE dbo.Languages ADD VoiceLocale NVARCHAR(35) NULL;
            IF COL_LENGTH(N'dbo.Languages', N'DisplayOrder') IS NULL
                ALTER TABLE dbo.Languages ADD DisplayOrder INT NOT NULL
                    CONSTRAINT DF_Languages_DisplayOrder DEFAULT (0);
            IF COL_LENGTH(N'dbo.POIs', N'PlayCount') IS NULL
                ALTER TABLE dbo.POIs ADD PlayCount INT NOT NULL
                    CONSTRAINT DF_POIs_PlayCount DEFAULT (0);
            IF COL_LENGTH(N'dbo.Translations', N'Status') IS NULL
                ALTER TABLE dbo.Translations ADD Status NVARCHAR(30) NOT NULL
                    CONSTRAINT DF_Translations_Status DEFAULT (N'Đã công bố');
            IF COL_LENGTH(N'dbo.Translations', N'DisplayOrder') IS NULL
                ALTER TABLE dbo.Translations ADD DisplayOrder INT NOT NULL
                    CONSTRAINT DF_Translations_DisplayOrder DEFAULT (0);
            IF COL_LENGTH(N'dbo.Audio_Files', N'SpeechRate') IS NULL
                ALTER TABLE dbo.Audio_Files ADD SpeechRate FLOAT NOT NULL
                    CONSTRAINT DF_Audio_Files_SpeechRate DEFAULT (1.0);
            IF COL_LENGTH(N'dbo.Audio_Files', N'Pitch') IS NULL
                ALTER TABLE dbo.Audio_Files ADD Pitch FLOAT NOT NULL
                    CONSTRAINT DF_Audio_Files_Pitch DEFAULT (1.0);
            IF COL_LENGTH(N'dbo.Audio_Files', N'Status') IS NULL
                ALTER TABLE dbo.Audio_Files ADD Status NVARCHAR(30) NOT NULL
                    CONSTRAINT DF_Audio_Files_Status DEFAULT (N'Đã công bố');
            IF COL_LENGTH(N'dbo.Audio_Files', N'DisplayOrder') IS NULL
                ALTER TABLE dbo.Audio_Files ADD DisplayOrder INT NOT NULL
                    CONSTRAINT DF_Audio_Files_DisplayOrder DEFAULT (0);
            IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Audio_Files')
                    AND name = N'AudioURL'
                    AND max_length <> -1
            )
                ALTER TABLE dbo.Audio_Files ALTER COLUMN AudioURL NVARCHAR(MAX) NOT NULL;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> IsRelationalStateReadyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE
                WHEN TRY_CONVERT(INT, JSON_VALUE(ApplicationDataJson, '$.relationalVersion')) >= 1 THEN 1
                ELSE 0
            END
            FROM dbo.Admin_Accounts
            WHERE AdminID = (SELECT MIN(AdminID) FROM dbo.Admin_Accounts);
            """;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int version && version >= 1;
    }

    public async Task MarkRelationalStateReadyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Admin_Accounts
            SET ApplicationDataJson = JSON_MODIFY(
                COALESCE(ApplicationDataJson, N'{}'),
                '$.relationalVersion',
                1)
            WHERE AdminID = (SELECT MIN(AdminID) FROM dbo.Admin_Accounts);
            """;
        var updatedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updatedRows != 1)
        {
            throw new InvalidOperationException("Could not mark the six-table SQL migration as complete.");
        }
    }

    public async Task<string?> LoadSectionAsync(
        string section,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1) ApplicationDataJson
            FROM dbo.Admin_Accounts
            WHERE TRY_CONVERT(INT, JSON_VALUE(ApplicationDataJson, '$.relationalVersion')) IS NULL
            ORDER BY AdminID;
            """;
        var storedValue = await command.ExecuteScalarAsync(cancellationToken);
        if (storedValue is not string json || string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidDataException("ApplicationDataJson in SQL Server must contain a JSON object.");
        return root[section]?.ToJsonString();
    }

    public async Task SaveSectionAsync(
        string section,
        string sectionJson,
        CancellationToken cancellationToken = default)
    {
        var sectionNode = JsonNode.Parse(sectionJson)
            ?? throw new InvalidDataException($"The '{section}' application data is not valid JSON.");

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                await using var readCommand = connection.CreateCommand();
                readCommand.Transaction = transaction;
                readCommand.CommandText = """
                    SELECT TOP (1) AdminID, ApplicationDataJson
                    FROM dbo.Admin_Accounts WITH (UPDLOCK, HOLDLOCK)
                    ORDER BY AdminID;
                    """;
                int adminId;
                JsonObject root;
                await using (var reader = await readCommand.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "Cannot persist application data: dbo.Admin_Accounts has no administrator row.");
                    }

                    adminId = reader.GetInt32(0);
                    var existingJson = reader.IsDBNull(1) ? null : reader.GetString(1);
                    root = string.IsNullOrWhiteSpace(existingJson)
                        ? new JsonObject()
                        : JsonNode.Parse(existingJson) as JsonObject
                            ?? throw new InvalidDataException("ApplicationDataJson in SQL Server must contain a JSON object.");
                }

                root[section] = sectionNode;

                await using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = """
                    UPDATE dbo.Admin_Accounts
                    SET ApplicationDataJson = @ApplicationDataJson
                    WHERE AdminID = @AdminID;
                    """;
                updateCommand.Parameters.AddWithValue("@ApplicationDataJson", root.ToJsonString());
                updateCommand.Parameters.AddWithValue("@AdminID", adminId);
                var updatedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                if (updatedRows != 1)
                {
                    throw new InvalidOperationException("Failed to persist application data in dbo.Admin_Accounts.");
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
