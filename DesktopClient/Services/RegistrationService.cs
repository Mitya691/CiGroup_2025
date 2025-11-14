using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DesktopClient.Services.IRegistrationService;

namespace DesktopClient.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly string _connectionString;
        private readonly IPasswordHasher _passwordHasher;

        public RegistrationService(string connectionString, IPasswordHasher passwordHasher)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<SignUpResult> SignUpAsync(SignUpRequest req, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync(ct);
            }
            catch
            {
            }

            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
               
                const string checkSql = @"
                SELECT EXISTS(SELECT 1 FROM auth_users WHERE Login=@login) AS login_taken,
                       EXISTS(SELECT 1 FROM users      WHERE Email=@mail ) AS email_taken;";
                bool loginTaken, emailTaken;
                await using (var check = new MySqlCommand(checkSql, conn, (MySqlTransaction)tx))
                {
                    check.Parameters.Add("@login", MySqlDbType.VarChar, 128).Value = req.Login;
                    check.Parameters.Add("@mail", MySqlDbType.VarChar, 255).Value = req.Email;
                    await using var r = await check.ExecuteReaderAsync(ct);
                    await r.ReadAsync(ct);
                    loginTaken = r.GetInt32("login_taken") == 1;
                    emailTaken = r.GetInt32("email_taken") == 1;
                }
                if (loginTaken) return new(false, null, "login_taken", "Логин уже занят");
                if (emailTaken) return new(false, null, "email_taken", "Email уже используется");

                const string insUser = @"
                INSERT INTO users(Name, Family, Patronymic, Email, Post)
                VALUES(@name, @family, @patronymic, @email, @post);
                SELECT LAST_INSERT_ID();";
                long userId;
                await using (var cmd = new MySqlCommand(insUser, conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.Add("@name", MySqlDbType.VarChar, 100).Value = req.Name;
                    cmd.Parameters.Add("@family", MySqlDbType.VarChar, 100).Value = req.Family;
                    cmd.Parameters.Add("@patronymic", MySqlDbType.VarChar, 100).Value = (object?)req.Patronymic ?? DBNull.Value;
                    cmd.Parameters.Add("@email", MySqlDbType.VarChar, 255).Value = req.Email;
                    cmd.Parameters.Add("@post", MySqlDbType.VarChar, 255).Value = req.Post;

                    var idObj = await cmd.ExecuteScalarAsync(ct);
                    userId = Convert.ToInt64(idObj);
                }

                const int iters = 100_000;
                var saltB64 = _passwordHasher.NewSalt(16);
                var hashB64 = _passwordHasher.Hash(req.Password, saltB64, iters);
                var salt = Convert.FromBase64String(saltB64);
                var hash = Convert.FromBase64String(hashB64);

                const string insAuth = @"
                INSERT INTO auth_users(EmployeeId, Login, PasswordSalt, PasswordHash)
                VALUES(@uid, @login, @salt, @hash);";
                await using (var cmd = new MySqlCommand(insAuth, conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.Add("@uid", MySqlDbType.Int64).Value = userId;
                    cmd.Parameters.Add("@login", MySqlDbType.VarChar, 128).Value = req.Login;
                    cmd.Parameters.Add("@salt", MySqlDbType.VarBinary, 16).Value = salt;
                    cmd.Parameters.Add("@hash", MySqlDbType.VarBinary, 32).Value = hash;
                    cmd.Parameters.Add("@it", MySqlDbType.Int32).Value = iters;
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
                return new(true, userId, null, "Пользователь создан");
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync(ct);
                // 1062 — дубликат (страхуемся, даже если пропустили ручную проверку)
                if (ex.Number == 1062) return new(false, null, "duplicate", "Логин или email уже заняты");
                return new(false, null, "db_error", "Ошибка БД: " + ex.Message);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return new(false, null, "unknown", "Неизвестная ошибка: " + ex.Message);
            }

        }
    }
}
