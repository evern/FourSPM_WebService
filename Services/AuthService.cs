using FourSPM_WebService.Config;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Helpers;
using FourSPM_WebService.Models.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Logging;

namespace FourSPM_WebService.Services;

public class AuthService : IAuthService
{
    private readonly JwtConfig _jwtConfig;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly MsalTokenValidator _msalTokenValidator;

    public AuthService(
        JwtConfig jwtConfig, 
        IConfiguration configuration, 
        ILogger<AuthService> logger,
        MsalTokenValidator msalTokenValidator)
    {
        _jwtConfig = jwtConfig;
        _configuration = configuration;
        _logger = logger;
        _msalTokenValidator = msalTokenValidator;
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
            // Determine if this is an MSAL token
            bool isMsalToken = IsMsalTokenFromString(token);
            
            if (isMsalToken)
            {
                return ValidateMsalToken(token);
            }
            else
            {
                return ValidateLegacyToken(token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }
    
    private bool ValidateLegacyToken(string token)
    {
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Legacy token validation failed");
            return false;
        }
    }
    
    private bool ValidateMsalToken(string token)
    {
        try
        {
            // Use the specialized validator for comprehensive MSAL token validation
            var validationTask = _msalTokenValidator.ValidateTokenAsync(token);
            validationTask.Wait(); // We need to block here since this method is synchronous
            var (isValid, _) = validationTask.Result;
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MSAL token validation failed");
            return false;
        }
    }

    public UserInfo? GetUserInfoFromToken(string token)
    {
        try
        {
            // First, determine if this is an MSAL token
            bool isMsalToken = IsMsalTokenFromString(token);
            
            if (isMsalToken)
            {
                return GetUserInfoFromMsalToken(token);
            }
            else
            {
                return GetUserInfoFromLegacyToken(token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from token");
        }

        return null;
    }
    
    private UserInfo? GetUserInfoFromLegacyToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
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
                    Email = nameClaim.Value, // Using username as email for now
                    AuthType = "Legacy"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from legacy token");
        }
        
        return null;
    }
    
    private UserInfo? GetUserInfoFromMsalToken(string token)
    {
        try
        {
            // First validate the token using our specialized validator
            var validationTask = _msalTokenValidator.ValidateTokenAsync(token);
            validationTask.Wait();
            var (isValid, principal) = validationTask.Result;
            
            if (!isValid || principal == null)
            {
                _logger.LogWarning("Could not validate MSAL token");
                return null;
            }
            
            // Extract user information from the validated principal
            string? objectId = principal.FindFirst("oid")?.Value ?? 
                            principal.FindFirst("sub")?.Value;
                           
            string? name = principal.FindFirst("name")?.Value ?? 
                        principal.FindFirst("upn")?.Value ?? 
                        principal.Identity?.Name;
                        
            string? email = principal.FindFirst("email")?.Value ?? 
                         principal.FindFirst("preferred_username")?.Value;
            
            if (!string.IsNullOrEmpty(objectId) && !string.IsNullOrEmpty(name))
            {
                if (!Guid.TryParse(objectId, out Guid userId))
                {
                    // Create a deterministic GUID based on the objectId string
                    // This ensures the same objectId always maps to the same GUID
                    userId = CreateDeterministicGuid(objectId);
                }
                
                return new UserInfo
                {
                    UserId = userId,
                    UserName = name,
                    Email = email ?? name,
                    AuthType = "MSAL",
                    ObjectId = objectId
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from MSAL token");
        }
        
        return null;
    }
    
    // Helper method to create a GUID from a string consistently
    private Guid CreateDeterministicGuid(string input)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(input));
            return new Guid(hash);
        }
    }

    public string GeneratePasswordResetToken(USER user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.GUID.ToString()),
                new Claim(ClaimTypes.Email, user.USERNAME),
                new Claim("purpose", "password_reset")
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Reset token valid for 1 hour
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public UserInfo? ValidatePasswordResetToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;

            if (jwtToken == null)
            {
                return null;
            }

            // Verify this is a password reset token
            var purposeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "purpose");
            if (purposeClaim?.Value != "password_reset")
            {
                return null;
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return new UserInfo
            {
                UserId = userId,
                Email = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task ValidateTokenAsync(TokenValidatedContext context)
    {
        try
        {
            // Extract the security token (JWT)
            var securityToken = context.SecurityToken as JwtSecurityToken;
            if (securityToken == null)
            {
                _logger.LogWarning("Token validation failed: Security token is not a JWT token");
                context.Fail("Invalid token format");
                return;
            }
            
            // Determine if this is an MSAL token based on issuer
            bool isMsalToken = IsMsalToken(securityToken);
            context.HttpContext.Items["IsMsalToken"] = isMsalToken;
            
            if (isMsalToken)
            {
                // Use specialized validator for MSAL tokens
                await _msalTokenValidator.ValidateTokenAsync(context);
            }
            else
            {
                // For legacy tokens, we've already done the validation in the JWT Bearer handler
                // but we could add additional custom logic here if needed
                _logger.LogInformation("Legacy token validated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token validation");
            context.Fail("Token validation failed");
        }
    }
    
    private bool IsMsalToken(JwtSecurityToken token)
    {
        // MSAL tokens are issued by Azure AD with issuers that match these patterns
        var tenantId = _configuration["AzureAd:TenantId"];
        var msalIssuers = new[]
        {
            $"https://login.microsoftonline.com/{tenantId}/v2.0",
            $"https://sts.windows.net/{tenantId}/"
        };
        
        return msalIssuers.Any(issuer => token.Issuer.StartsWith(issuer));
    }
    
    private bool IsMsalTokenFromString(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return false;
            }
            
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return IsMsalToken(jwtToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error determining token type");
            return false;
        }
    }
    
    // This method is no longer needed as we're using the MsalTokenValidator
}