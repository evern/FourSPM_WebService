using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace FourSPM_WebService.Data.Extensions
{
    public class EdmModelBuilder
    {
        internal static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EnableLowerCamelCase();

            builder.EntitySet<ProjectEntity>("Projects").EntityType.HasKey(p => p.Guid);
            builder.EntitySet<UserEntity>("Users");
            
            // Add new entity sets
            builder.EntitySet<DepartmentEntity>("Departments").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<DeliverableEntity>("Deliverables").EntityType.HasKey(d => d.Guid);
            builder.EntitySet<ProgressEntity>("Progress").EntityType.HasKey(p => p.Guid);
            builder.EntitySet<ClientEntity>("Clients").EntityType.HasKey(c => c.Guid);

            return builder.GetEdmModel();
        }
    }
}
