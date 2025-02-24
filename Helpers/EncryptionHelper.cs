using System.Security.Cryptography;

namespace FourSPM_WebService.Helpers
{
    public static class EncryptionHelper
    {
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 100000;

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>Base64 encoded string containing the salt and hash</returns>
        public static string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[KeySize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = deriveBytes.GetBytes(KeySize);

            // Combine salt and hash
            byte[] hashBytes = new byte[KeySize * 2];
            Array.Copy(salt, 0, hashBytes, 0, KeySize);
            Array.Copy(hash, 0, hashBytes, KeySize, KeySize);

            // Convert to base64 and return
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a password against its hash
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <param name="storedHash">The stored hash to verify against</param>
        /// <returns>True if the password matches, false otherwise</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Get the complete hash data
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                // Extract the salt (first KeySize bytes)
                byte[] salt = new byte[KeySize];
                Array.Copy(hashBytes, 0, salt, 0, KeySize);

                // Extract the hash (remaining bytes)
                byte[] hash = new byte[KeySize];
                Array.Copy(hashBytes, KeySize, hash, 0, KeySize);

                // Hash the input password with the same salt
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] newHash = deriveBytes.GetBytes(KeySize);

                // Compare the hashes
                return newHash.SequenceEqual(hash);
            }
            catch
            {
                return false;
            }
        }
    }
}