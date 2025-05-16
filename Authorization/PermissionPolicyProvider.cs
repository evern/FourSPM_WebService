using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// A policy provider that creates authorization policies for permission requirements
    /// </summary>
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
        
        /// <summary>
        /// Initializes a new instance of <see cref="PermissionPolicyProvider"/>
        /// </summary>
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <summary>
        /// Gets the default authorization policy
        /// </summary>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        /// <summary>
        /// Gets the fallback authorization policy
        /// </summary>
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

        /// <summary>
        /// Gets a policy for the specified policy name
        /// </summary>
        /// <param name="policyName">The policy name, which is the permission name</param>
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // If the policy name doesn't match any of our permission patterns,
            // use the fallback provider
            if (string.IsNullOrWhiteSpace(policyName) ||
                !policyName.Contains('.'))
            {
                return _fallbackPolicyProvider.GetPolicyAsync(policyName);
            }

            // Create a policy with the permission requirement
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }
    }
}
