using Microsoft.Data.SqlClient;
using NewVK.Models;

namespace NewVK.Data
{
    public sealed class UsersRepository
    {
        private readonly AppDbConnectionFactory _connectionFactory;

        public UsersRepository(AppDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Users
WHERE NormalizedLogin = @NormalizedLogin;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            AddNVarChar(command, "@NormalizedLogin", 50, Normalize(login));

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Users
WHERE NormalizedEmail = @NormalizedEmail;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            AddNVarChar(command, "@NormalizedEmail", 255, Normalize(email));

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> LoginExistsForOtherAsync(int userId, string login, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Users
WHERE NormalizedLogin = @NormalizedLogin
  AND Id <> @Id;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            AddNVarChar(command, "@NormalizedLogin", 50, Normalize(login));
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = userId });

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> EmailExistsForOtherAsync(int userId, string email, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Users
WHERE NormalizedEmail = @NormalizedEmail
  AND Id <> @Id;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            AddNVarChar(command, "@NormalizedEmail", 255, Normalize(email));
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = userId });

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<AppUser?> GetByLoginOrEmailAsync(string loginOrEmail, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT TOP(1)
    Id,
    Login,
    FirstName,
    LastName,
    Email,
    Phone,
    AboutMe,
    ThemeKey,
    PasswordHash,
    PasswordSalt
FROM dbo.Users
WHERE NormalizedLogin = @Value
   OR NormalizedEmail = @Value;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            AddNVarChar(command, "@Value", 255, Normalize(loginOrEmail));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapUser(reader);
        }

        public async Task<AppUser?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT TOP(1)
    Id,
    Login,
    FirstName,
    LastName,
    Email,
    Phone,
    AboutMe,
    ThemeKey,
    PasswordHash,
    PasswordSalt
FROM dbo.Users
WHERE Id = @Id;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = userId });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapUser(reader);
        }

        public async Task<int> CreateAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.Users
(
    Login,
    NormalizedLogin,
    FirstName,
    LastName,
    Email,
    NormalizedEmail,
    Phone,
    AboutMe,
    ThemeKey,
    PasswordHash,
    PasswordSalt,
    CreatedAtUtc
)
VALUES
(
    @Login,
    @NormalizedLogin,
    @FirstName,
    @LastName,
    @Email,
    @NormalizedEmail,
    @Phone,
    @AboutMe,
    @ThemeKey,
    @PasswordHash,
    @PasswordSalt,
    SYSUTCDATETIME()
);

SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            AddNVarChar(command, "@Login", 50, request.Login.Trim());
            AddNVarChar(command, "@NormalizedLogin", 50, Normalize(request.Login));
            AddNVarChar(command, "@FirstName", 100, request.FirstName.Trim());
            AddNVarChar(command, "@LastName", 100, request.LastName.Trim());
            AddNVarChar(command, "@Email", 255, request.Email.Trim());
            AddNVarChar(command, "@NormalizedEmail", 255, Normalize(request.Email));
            AddNullableNVarChar(command, "@Phone", 30, request.Phone);
            AddNullableNVarChar(command, "@AboutMe", 1000, request.AboutMe);
            AddNVarChar(command, "@ThemeKey", 50, string.IsNullOrWhiteSpace(request.ThemeKey) ? SiteThemeDefaults.DefaultKey : request.ThemeKey);
            AddNVarChar(command, "@PasswordHash", 256, request.PasswordHash);
            AddNVarChar(command, "@PasswordSalt", 256, request.PasswordSalt);

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        public async Task UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE dbo.Users
SET
    Login = @Login,
    NormalizedLogin = @NormalizedLogin,
    FirstName = @FirstName,
    LastName = @LastName,
    Email = @Email,
    NormalizedEmail = @NormalizedEmail,
    Phone = @Phone,
    AboutMe = @AboutMe,
    ThemeKey = @ThemeKey
WHERE Id = @Id;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = request.Id });

            AddNVarChar(command, "@Login", 50, request.Login.Trim());
            AddNVarChar(command, "@NormalizedLogin", 50, Normalize(request.Login));
            AddNVarChar(command, "@FirstName", 100, request.FirstName.Trim());
            AddNVarChar(command, "@LastName", 100, request.LastName.Trim());
            AddNVarChar(command, "@Email", 255, request.Email.Trim());
            AddNVarChar(command, "@NormalizedEmail", 255, Normalize(request.Email));
            AddNullableNVarChar(command, "@Phone", 30, request.Phone);
            AddNullableNVarChar(command, "@AboutMe", 1000, request.AboutMe);
            AddNVarChar(command, "@ThemeKey", 50, string.IsNullOrWhiteSpace(request.ThemeKey) ? SiteThemeDefaults.DefaultKey : request.ThemeKey);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE dbo.Users
SET LastLoginAtUtc = SYSUTCDATETIME()
WHERE Id = @Id;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = userId });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static AppUser MapUser(SqlDataReader reader)
        {
            int idIndex = reader.GetOrdinal("Id");
            int loginIndex = reader.GetOrdinal("Login");
            int firstNameIndex = reader.GetOrdinal("FirstName");
            int lastNameIndex = reader.GetOrdinal("LastName");
            int emailIndex = reader.GetOrdinal("Email");
            int phoneIndex = reader.GetOrdinal("Phone");
            int aboutMeIndex = reader.GetOrdinal("AboutMe");
            int themeKeyIndex = reader.GetOrdinal("ThemeKey");
            int passwordHashIndex = reader.GetOrdinal("PasswordHash");
            int passwordSaltIndex = reader.GetOrdinal("PasswordSalt");

            return new AppUser
            {
                Id = reader.GetInt32(idIndex),
                Login = reader.GetString(loginIndex),
                FirstName = reader.GetString(firstNameIndex),
                LastName = reader.GetString(lastNameIndex),
                Email = reader.GetString(emailIndex),
                Phone = reader.IsDBNull(phoneIndex) ? "" : reader.GetString(phoneIndex),
                AboutMe = reader.IsDBNull(aboutMeIndex) ? null : reader.GetString(aboutMeIndex),
                ThemeKey = reader.IsDBNull(themeKeyIndex) ? SiteThemeDefaults.DefaultKey : reader.GetString(themeKeyIndex),
                PasswordHash = reader.GetString(passwordHashIndex),
                PasswordSalt = reader.GetString(passwordSaltIndex)
            };
        }

        private static string Normalize(string value)
            => (value ?? string.Empty).Trim().ToUpperInvariant();

        private static void AddNVarChar(SqlCommand command, string name, int size, string value)
        {
            command.Parameters.Add(
                new SqlParameter(name, System.Data.SqlDbType.NVarChar, size)
                {
                    Value = value
                });
        }

        private static void AddNullableNVarChar(SqlCommand command, string name, int size, string? value)
        {
            command.Parameters.Add(
                new SqlParameter(name, System.Data.SqlDbType.NVarChar, size)
                {
                    Value = string.IsNullOrWhiteSpace(value)
                        ? DBNull.Value
                        : value.Trim()
                });
        }
    }
}