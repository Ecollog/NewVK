using Microsoft.Data.SqlClient;
using NewVK.Models;

namespace NewVK.Data
{
    public sealed class UserPhotosRepository
    {
        private readonly AppDbConnectionFactory _connectionFactory;

        public UserPhotosRepository(AppDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<UserPhoto>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT
    Id,
    UserId,
    FileName,
    OriginalFileName,
    RelativeUrl,
    ContentType,
    SizeBytes,
    UploadedAtUtc
FROM dbo.UserPhotos
WHERE UserId = @UserId
ORDER BY UploadedAtUtc DESC;";

            var result = new List<UserPhoto>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.Int) { Value = userId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(Map(reader));
            }

            return result;
        }

        public async Task<UserPhoto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT TOP(1)
    Id,
    UserId,
    FileName,
    OriginalFileName,
    RelativeUrl,
    ContentType,
    SizeBytes,
    UploadedAtUtc
FROM dbo.UserPhotos
WHERE Id = @Id
  AND UserId = @UserId;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = id });
            command.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.Int) { Value = userId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return Map(reader);
        }

        public async Task<int> CreateAsync(UserPhoto photo, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.UserPhotos
(
    UserId,
    FileName,
    OriginalFileName,
    RelativeUrl,
    ContentType,
    SizeBytes,
    UploadedAtUtc
)
VALUES
(
    @UserId,
    @FileName,
    @OriginalFileName,
    @RelativeUrl,
    @ContentType,
    @SizeBytes,
    SYSUTCDATETIME()
);

SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.Int) { Value = photo.UserId });
            AddNVarChar(command, "@FileName", 260, photo.FileName);
            AddNVarChar(command, "@OriginalFileName", 260, photo.OriginalFileName);
            AddNVarChar(command, "@RelativeUrl", 500, photo.RelativeUrl);
            AddNVarChar(command, "@ContentType", 100, photo.ContentType);
            command.Parameters.Add(new SqlParameter("@SizeBytes", System.Data.SqlDbType.BigInt) { Value = photo.SizeBytes });

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
DELETE FROM dbo.UserPhotos
WHERE Id = @Id
  AND UserId = @UserId;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = id });
            command.Parameters.Add(new SqlParameter("@UserId", System.Data.SqlDbType.Int) { Value = userId });

            int affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        private static UserPhoto Map(SqlDataReader reader)
        {
            return new UserPhoto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                RelativeUrl = reader.GetString(reader.GetOrdinal("RelativeUrl")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                SizeBytes = reader.GetInt64(reader.GetOrdinal("SizeBytes")),
                UploadedAtUtc = reader.GetDateTime(reader.GetOrdinal("UploadedAtUtc"))
            };
        }

        private static void AddNVarChar(SqlCommand command, string name, int size, string value)
        {
            command.Parameters.Add(
                new SqlParameter(name, System.Data.SqlDbType.NVarChar, size)
                {
                    Value = value
                });
        }
    }
}