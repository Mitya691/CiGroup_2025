using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Services
{
    public sealed record SignUpRequest(
            string Name,
            string Family,
            string Patronymic,
            string Email,
            string Post,
            string Login,
            string Password);

    public sealed record SignUpResult(
        bool Success,
        long? UserId = null,
        string? ErrorCode = null,   // "login_taken", "email_taken", "weak_password"
        string? Message = null);
   
    public interface IRegistrationService
    {
        Task<SignUpResult> SignUpAsync(SignUpRequest req, CancellationToken ct = default);
    }
}
