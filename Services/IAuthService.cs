using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Auth;

namespace FourSPM_WebService.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(USER user);
        bool VerifyPassword(string plainPassword, string storedHash);
        bool ValidateToken(string token);
        UserInfo? GetUserInfoFromToken(string token);
    }
}
