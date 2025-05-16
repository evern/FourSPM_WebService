using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FourSPM_WebService.Authorization
{
    /// <summary>
    /// Extension methods for configuring authorization services
    /// </summary>
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Adds the permission-based authorization services to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)
        {
            // Register authorization handler
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            
            // Register permission policy provider
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            
            return services;
        }
    }
}
