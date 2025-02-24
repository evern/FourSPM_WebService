using FourSPM_WebService.Config;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Helpers;
using FourSPM_WebService.Models.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FourSPM_WebService.Services;

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
            new Claim(ClaimTypes.Name, user.USERNAME),
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
        return EncryptionHelper.VerifyPassword(plainPassword, storedHash);
    }

    public string HashPassword(string password)
    {
        return EncryptionHelper.HashPassword(password);
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public UserInfo? GetUserInfoFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            var nameIdentifierClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            var nameClaim = principal.FindFirst(ClaimTypes.Name);

            if (nameIdentifierClaim != null && nameClaim != null && Guid.TryParse(nameIdentifierClaim.Value, out Guid userId))
            {
                return new UserInfo
                {
                    UserId = userId,
                    UserName = nameClaim.Value,
                    Email = nameClaim.Value // Using username as email for now
                };
            }
        }
        catch
        {
            // Log error here
        }

        return null;
    }
}