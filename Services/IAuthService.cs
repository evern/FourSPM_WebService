using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;

namespace FourSPM_WebService.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(USER user);
        string HashPassword(string password);
        bool VerifyPassword(string plainPassword, string storedHash);
        bool ValidateToken(string token);
        UserInfo? GetUserInfoFromToken(string token);
        string GeneratePasswordResetToken(USER user);
        UserInfo? ValidatePasswordResetToken(string token);
        Task ValidateTokenAsync(TokenValidatedContext context);
    }
}
