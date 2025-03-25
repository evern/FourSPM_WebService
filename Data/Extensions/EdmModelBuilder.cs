using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using FourSPM_WebService.Data.EF.FourSPM;

namespace FourSPM_WebService.Data.Extensions
{
    public class EdmModelBuilder
    {
        internal static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EnableLowerCamelCase();

            // Register enums with OData
            // This allows OData to serialize/deserialize enum values as strings (e.g., "Task", "Deliverable")
            // rather than numeric values, making the API more readable and robust to enum changes
            builder.EnumType<DeliverableTypeEnum>();
            builder.EnumType<DepartmentEnum>();

            // Configure Project entity with navigation properties
            var projectEntityType = builder.EntitySet<ProjectEntity>("Projects").EntityType;
            projectEntityType.HasKey(p => p.Guid);
            
            // Explicitly configure the Client navigation property
            projectEntityType.HasOptional(p => p.Client);
            
            // Configure Deliverables collection navigation property
            projectEntityType.HasMany(p => p.Deliverables);
            
            builder.EntitySet<UserEntity>("Users");
            
            // Add entity sets
            builder.EntitySet<DeliverableEntity>("Deliverables").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<ProgressEntity>("Progress").EntityType.HasKey(p => p.Guid);
            
            // Configure Client entity
            var clientEntityType = builder.EntitySet<ClientEntity>("Clients").EntityType;
            clientEntityType.HasKey(c => c.Guid);
            
            builder.EntitySet<DisciplineEntity>("Disciplines").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<DocumentTypeEntity>("DocumentTypes").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<AreaEntity>("Areas").EntityType.HasKey(a => a.Guid);
            builder.EntitySet<DeliverableGateEntity>("DeliverableGates").EntityType.HasKey(dg => dg.Guid);

            return builder.GetEdmModel();
        }
    }
}
