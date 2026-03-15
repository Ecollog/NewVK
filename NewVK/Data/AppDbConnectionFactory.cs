using Microsoft.Data.SqlClient;

namespace NewVK.Data
{
    public sealed class AppDbConnectionFactory
    {
        private readonly string _connectionString;

        public AppDbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Не найдена строка подключения 'DefaultConnection'.");
        }

        public SqlConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}