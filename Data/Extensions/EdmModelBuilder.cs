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

            // Register enums
            builder.EnumType<DeliverableTypeEnum>();
            builder.EnumType<DepartmentEnum>();

            builder.EntitySet<ProjectEntity>("Projects").EntityType.HasKey(p => p.Guid);
            builder.EntitySet<UserEntity>("Users");
            
            // Add entity sets
            builder.EntitySet<DeliverableEntity>("Deliverables").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<ProgressEntity>("Progress").EntityType.HasKey(p => p.Guid);
            builder.EntitySet<ClientEntity>("Clients").EntityType.HasKey(c => c.Guid);
            builder.EntitySet<DisciplineEntity>("Disciplines").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<DocumentTypeEntity>("DocumentTypes").EntityType.HasKey(d => d.Guid);

            return builder.GetEdmModel();
        }
    }
}
