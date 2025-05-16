using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FourSPM_WebService.Services;

public class MsalTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MsalTokenValidator> _logger;
    private readonly HttpClient _httpClient;
    
    public MsalTokenValidator(IConfiguration configuration, IMemoryCache cache, ILogger<MsalTokenValidator> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MsalValidator");
    }
    
    public async Task<(bool isValid, ClaimsPrincipal? principal)> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Empty or null token provided for validation");
            return (false, null);
        }
        try
        {
            // Get signing keys from Azure AD
            var signingKeys = await GetSigningKeysAsync();
            if (signingKeys == null || !signingKeys.Any())
            {
                _logger.LogError("Could not retrieve signing keys from Azure AD");
                return (false, null);
            }
            
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] 
                { 
                    $"https://login.microsoftonline.com/{tenantId}/v2.0",
                    $"https://sts.windows.net/{tenantId}/"
                },
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys
            };
            
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // MSAL-specific validation
            if (!principal.HasClaim(c => c.Type == "oid") && !principal.HasClaim(c => c.Type == "sub"))
            {
                _logger.LogWarning("MSAL token missing required identity claims (oid/sub)");
                return (false, null);
            }
            
            // Validate scopes (if configured)
            if (!await ValidateTokenScopesAsync(principal))
            {
                return (false, null);
            }
            
            // Additional validations could be added here
            // For example, validating organization membership, role claims, etc.
            
            return (true, principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSAL token validation failed");
            return (false, null);
        }
    }
    
    private Task<bool> ValidateTokenScopesAsync(ClaimsPrincipal principal)
    {
        // Get required scopes from configuration
        var requiredScopes = _configuration.GetSection("AzureAd:Scopes").Get<string[]>();
        if (requiredScopes == null || requiredScopes.Length == 0)
        {
            // No specific scopes required
            return Task.FromResult(true);
        }
        
        // Get scopes from token
        var scopeClaim = principal.FindFirst("scp") ?? 
                      principal.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
                      
        if (scopeClaim == null)
        {
            _logger.LogWarning("MSAL token has no scope claim");
            return Task.FromResult(false);
        }
        
        var tokenScopes = scopeClaim.Value.Split(' ');
        
        // Check if the token contains at least one of the required scopes
        var hasRequiredScope = requiredScopes.Any(rs => tokenScopes.Contains(rs));
        
        if (!hasRequiredScope)
        {
            _logger.LogWarning($"MSAL token missing required scopes. " +
                            $"Required: {string.Join(", ", requiredScopes)}, " +
                            $"Found: {string.Join(", ", tokenScopes)}");
        }
        
        return Task.FromResult(hasRequiredScope);
    }
    
    private async Task<IEnumerable<SecurityKey>?> GetSigningKeysAsync()
    {
        const string cacheKey = "MsalSigningKeys";
        
        // Try to get signing keys from cache first
        if (_cache.TryGetValue(cacheKey, out IEnumerable<SecurityKey>? cachedKeys))
        {
            return cachedKeys;
        }
        
        try
        {
            // Fetch OpenID configuration and signing keys from Microsoft
            var tenantId = _configuration["AzureAd:TenantId"];
            var openIdConfigUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
            
            var response = await _httpClient.GetAsync(openIdConfigUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get OpenID configuration: {StatusCode}", response.StatusCode);
                return null;
            }
            
            var openIdConfigJson = await response.Content.ReadAsStringAsync();
            var openIdConfig = JsonDocument.Parse(openIdConfigJson);
            
            if (!openIdConfig.RootElement.TryGetProperty("jwks_uri", out var jwksUriElement))
            {
                _logger.LogError("OpenID configuration does not contain jwks_uri");
                return null;
            }
            
            var jwksUri = jwksUriElement.GetString();
            if (string.IsNullOrEmpty(jwksUri))
            {
                _logger.LogError("jwks_uri is null or empty");
                return null;
            }
            
            // At this point jwksUri is guaranteed to be non-null
            string finalJwksUri = jwksUri;
            
            var jwksResponse = await _httpClient.GetAsync(finalJwksUri);
            if (!jwksResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get JWKS: {StatusCode}", jwksResponse.StatusCode);
                return null;
            }
            
            var jwksJson = await jwksResponse.Content.ReadAsStringAsync();
            var jwks = new JsonWebKeySet(jwksJson);
            var keys = jwks.Keys.Select(k => (SecurityKey)k).ToList();
            
            // Cache the keys for 24 hours
            // Azure AD rotates keys every ~24 hours
            _cache.Set(cacheKey, keys, TimeSpan.FromHours(24));
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signing keys");
            return null;
        }
    }
    
    // Helper method to validate MSAL token in TokenValidatedContext
    public async Task ValidateTokenAsync(TokenValidatedContext context)
    {
        var token = context.SecurityToken as JwtSecurityToken;
        if (token == null)
        {
            context.Fail("Invalid token format");
            return;
        }
        
        // We use our specialized validation logic
        // Ensure we have a valid token string
        var tokenString = token.RawData;
        if (string.IsNullOrEmpty(tokenString))
        {
            context.Fail("Could not get raw token data");
            return;
        }
        
        var (isValid, principal) = await ValidateTokenAsync(tokenString);
        
        if (!isValid)
        {
            context.Fail("MSAL token validation failed");
            return;
        }
        
        // Additional validation for app-specific requirements can be done here
        var objectId = principal!.FindFirst("oid")?.Value ?? principal.FindFirst("sub")?.Value;
        _logger.LogInformation("Validated MSAL token for user with object ID: {ObjectId}", objectId);
    }
}
