using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.Repositories;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourSPM_WebService.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register ApplicationUser as scoped
            services.AddScoped<ApplicationUser>();

            // Register repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
