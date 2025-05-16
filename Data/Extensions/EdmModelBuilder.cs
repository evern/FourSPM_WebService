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
            builder.EnumType<VariationStatus>();

            // Configure Project entity with navigation properties
            var projectEntityType = builder.EntitySet<ProjectEntity>("Projects").EntityType;
            projectEntityType.HasKey(p => p.Guid);
            
            // Explicitly configure the Client navigation property
            projectEntityType.HasOptional(p => p.Client);
            
            // Configure Deliverables collection navigation property
            projectEntityType.HasMany(p => p.Deliverables);
            
            builder.EntitySet<UserEntity>("Users");
            
            // Add entity sets
            var deliverableEntityType = builder.EntitySet<DeliverableEntity>("Deliverables").EntityType;
            deliverableEntityType.HasKey(d => d.Guid);
            
            // Define the GetWithProgressPercentages function as a bound OData function on the Deliverables collection
            // This ensures proper OData serialization including enum string values
            var getProgressFunction = deliverableEntityType.Collection
                .Function("GetWithProgressPercentages")
                .ReturnsCollectionFromEntitySet<DeliverableEntity>("Deliverables");
            
            // Add parameters to the function
            getProgressFunction.Parameter<Guid>("projectGuid");
            getProgressFunction.Parameter<int>("period");
            
            builder.EntitySet<ProgressEntity>("Progress").EntityType.HasKey(p => p.Guid);
            
            // Configure Client entity
            var clientEntityType = builder.EntitySet<ClientEntity>("Clients").EntityType;
            clientEntityType.HasKey(c => c.Guid);
            
            builder.EntitySet<DisciplineEntity>("Disciplines").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<DocumentTypeEntity>("DocumentTypes").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<AreaEntity>("Areas").EntityType.HasKey(a => a.Guid);
            builder.EntitySet<DeliverableGateEntity>("DeliverableGates").EntityType.HasKey(dg => dg.Guid);
            builder.EntitySet<VariationEntity>("Variations").EntityType.HasKey(v => v.Guid);
            builder.EntitySet<RoleEntity>("Roles").EntityType.HasKey(r => r.Guid);
            
            // Register VariationDeliverables - using DeliverableEntity type but different endpoint
            // This ensures that variation deliverables can be separately queried while maintaining the same model
            var variationDeliverableEntityType = builder.EntitySet<DeliverableEntity>("VariationDeliverables").EntityType;
            variationDeliverableEntityType.HasKey(d => d.Guid);
            
            // Define the GetMergedVariationDeliverables function as a bound OData function on the VariationDeliverables collection
            // This follows the same pattern as GetWithProgressPercentages
            var getMergedDeliverables = variationDeliverableEntityType.Collection
                .Function("GetMergedVariationDeliverables")
                .ReturnsCollectionFromEntitySet<DeliverableEntity>("VariationDeliverables");
            
            // Add parameter to the function
            getMergedDeliverables.Parameter<Guid>("variationId");

            return builder.GetEdmModel();
        }
    }
}
