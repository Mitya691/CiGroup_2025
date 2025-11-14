using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Data.SqlTypes;

namespace DesktopClient.Services
{
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        public string NewSalt(int size = 16)
        {
            var salt = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(salt);
        }

        public string Hash(string password, string base64Salt, int iterations = 100000)
        {
            var salt = Convert.FromBase64String(base64Salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(key);
        }

        public bool Verify(string password, string base64Salt, string base64Hash, int iterations = 100000)
        {
            var hash = Hash(password, base64Salt, iterations);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hash),
                Convert.FromBase64String(base64Hash));
        }
    }
}
