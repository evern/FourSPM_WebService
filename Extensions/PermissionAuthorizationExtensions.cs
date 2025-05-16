using FourSPM_WebService.Authorization;
using FourSPM_WebService.Config;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace FourSPM_WebService.Extensions
{
    /// <summary>
    /// Extensions for configuring permission-based authorization
    /// </summary>
    public static class PermissionAuthorizationExtensions
    {
        /// <summary>
        /// Adds permission-based authorization policies to the authorization options
        /// </summary>
        public static AuthorizationOptions AddPermissionPolicies(this AuthorizationOptions options)
        {
            // Get all permission constants from AuthConstants.Permissions using reflection
            var permissionFields = typeof(AuthConstants.Permissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

            // Register a policy for each permission
            foreach (var field in permissionFields)
            {
                string? permissionName = field.GetRawConstantValue() as string;
                if (!string.IsNullOrEmpty(permissionName))
                {
                    // Create a policy for each permission
                    options.AddPolicy($"Permission:{permissionName}", policy =>
                    {
                        policy.Requirements.Add(new RequirePermissionRequirement(permissionName));
                    });
                }
            }

            return options;
        }
    }
}
