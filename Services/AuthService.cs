using FourSPM_WebService.Config;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FourSPM_WebService.Services;

public interface IAuthService
{
    string GenerateJwtToken(USER user);
    bool VerifyPassword(string plainPassword, string storedHash);
}

public class AuthService : IAuthService
{
    private readonly JwtConfig _jwtConfig;

    public AuthService(JwtConfig jwtConfig)
    {
        _jwtConfig = jwtConfig;
    }

    public string GenerateJwtToken(USER user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.NameIdentifier, user.GUID.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_jwtConfig.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool VerifyPassword(string plainPassword, string storedHash)
    {
        return PasswordHasher.VerifyPassword(plainPassword, storedHash);
    }
}