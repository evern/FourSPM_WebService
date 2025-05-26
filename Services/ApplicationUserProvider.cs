using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Interfaces;

namespace FourSPM_WebService.Services
{
    /// <summary>
    /// Provider for accessing the current ApplicationUser populated from MSAL claims.
    /// This service also handles user identity mapping for consistent audit tracking.
    /// </summary>
    public class ApplicationUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserIdentityMappingRepository _repository;
        private readonly ILogger<ApplicationUserProvider> _logger;

        public ApplicationUserProvider(
            IHttpContextAccessor httpContextAccessor,
            IUserIdentityMappingRepository repository,
            ILogger<ApplicationUserProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a user identity mapping from MSAL claims
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal from MSAL authentication</param>
        /// <returns>The user ID that can be used for audit fields</returns>
        public async Task<Guid> GetOrCreateUserFromClaimsAsync(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null || claimsPrincipal.Identity == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Attempted to get or create user from unauthenticated claims");
                return Guid.Empty;
            }

            try
            {
                // Extract username (preferred_username is available in both work and personal accounts)
                string? username = claimsPrincipal.FindFirst(c => c.Type.ToLower() == "name")?.Value;
                           
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("No name claim found in authenticated user");
                    return Guid.Empty;
                }

                // Check if user already exists
                var existingMapping = await _repository.GetByUsernameAsync(username);
                if (existingMapping != null)
                {
                    // Update last login
                    await _repository.UpdateLastLoginAsync(existingMapping.GUID);
                    return existingMapping.GUID;
                }

                // Create new user mapping
                var newMapping = new USER_IDENTITY_MAPPING
                {
                    GUID = Guid.NewGuid(),
                    // For username, use the name claim we already extracted
                    USERNAME = username,
                    // For email, try to get the email claim or fall back to the name we found
                    EMAIL = claimsPrincipal.FindFirst("email")?.Value ?? username,
                    CREATED = DateTime.Now,
                    LAST_LOGIN = DateTime.Now
                };

                var createdMapping = await _repository.CreateAsync(newMapping);
                return createdMapping.GUID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating user identity mapping from claims");
                return Guid.Empty;
            }
        }
    }
}
