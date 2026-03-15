using System.Security.Cryptography;

namespace NewVK.Security
{
    public sealed class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public PasswordHashResult Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                Algorithm,
                KeySize);

            return new PasswordHashResult(
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt));
        }

        public bool Verify(string password, string storedHash, string storedSalt)
        {
            try
            {
                byte[] salt = Convert.FromBase64String(storedSalt);
                byte[] expectedHash = Convert.FromBase64String(storedHash);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    Algorithm,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }

    public sealed record PasswordHashResult(string Hash, string Salt);
}