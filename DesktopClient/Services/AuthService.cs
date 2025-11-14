using DesktopClient.Model;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DesktopClient.Services
{
    internal class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly IPasswordHasher _passwordHasher;

        private User? _user;

        private readonly object _sync = new();

        public AuthService(string connectionString, IPasswordHasher passwordHasher) //конструктор сервиса, зависит от IPasswordHasher и CS, поэтому регистрируем сервис в app.xaml.cs
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<bool> SignInAsync(string login, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return false;

            const string SqlAuth = @"SELECT ID, PasswordSalt, PasswordHash, IsActive
                                     FROM auth_users WHERE Login = @login
                                     LIMIT 1;";

            const string SqlBadge = @" SELECT Family, Name, Patronymic, Post
                                       FROM users WHERE ID = @id
                                       LIMIT 1;";

            byte[] saltBytes = null;
            byte[] hashBytes = null;
            int id = 0;
            int iterations = 100000;
            
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using (var cmd = new MySqlCommand(SqlAuth, conn))
            {
                cmd.Parameters.AddWithValue("@login", login);

                await using var rd = await cmd.ExecuteReaderAsync(ct);
                if (await rd.ReadAsync(ct))
                {
                    id = rd.GetInt32("ID");
                    saltBytes = (byte[])rd["PasswordSalt"];
                    hashBytes = (byte[])rd["PasswordHash"];
                }  
            }

            var base64Salt = Convert.ToBase64String(saltBytes);
            var base64Hash = Convert.ToBase64String(hashBytes);
            var ok = _passwordHasher.Verify(password, base64Salt, base64Hash);

            if (!ok) return false;

            User? userBadge = null;

            await using (var cmd = new MySqlCommand(SqlBadge, conn))
            {
                cmd.Parameters.AddWithValue("@id", id+36);
                await using var rd = await cmd.ExecuteReaderAsync(ct);
                if (await rd.ReadAsync(ct))
                {
                    userBadge = new User
                    {
                        Name = rd.GetString("Name"),
                        Family = rd.GetString("Family"),
                        Patronymic = rd.GetString("Patronymic"),
                        Post = rd.GetString("Post")
                    };
                }
            }

            userBadge ??= new User { Login =  login };

            lock (_sync) _user = userBadge;

            return true;
        }

        public Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
        {
            User? snapshot;
            lock (_sync) snapshot = _user;
            return Task.FromResult(snapshot);
        }

        public Task SignOutAsync(CancellationToken ct = default)
        {
            lock (_sync)
            {
                _user = null;
            }
            return Task.CompletedTask;
        }
    }
}