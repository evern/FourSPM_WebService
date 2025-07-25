using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Data.Interfaces;
using FourSPM_WebService.Data.Repositories;
using Microsoft.AspNetCore.Http;
using FourSPM_WebService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourSPM_WebService.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register HttpContextAccessor for accessing the current request
            // Still needed for other services and controllers
            services.AddHttpContextAccessor();
            
            // Register ApplicationUserProvider as scoped to match IUserIdentityMappingRepository lifetime
            services.AddScoped<ApplicationUserProvider>();
            
            // Register a default ApplicationUser for repositories
            // Controllers will still use their own CurrentUser property
            services.AddSingleton<ApplicationUser>(new ApplicationUser());
            
            // Register repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            
            // Register new repositories
            services.AddScoped<IDeliverableRepository, DeliverableRepository>();
            services.AddScoped<IProgressRepository, ProgressRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IDisciplineRepository, DisciplineRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IAreaRepository, AreaRepository>();
            services.AddScoped<IDeliverableGateRepository, DeliverableGateRepository>();
            services.AddScoped<IVariationRepository, VariationRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserIdentityMappingRepository, UserIdentityMappingRepository>();

            return services;
        }
    }
}
