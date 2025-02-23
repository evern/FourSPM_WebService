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

            var projectsType = builder.EntitySet<ProjectEntity>("Projects").EntityType;
            projectsType.HasKey(p => p.Guid); // Ensure OData has a primary key

            // Define properties (not needed unless customization required)
            projectsType.Property(p => p.ClientNumber);
            projectsType.Property(p => p.ProjectNumber);
            projectsType.Property(p => p.Name);
            projectsType.Property(p => p.ClientContact);
            projectsType.Property(p => p.PurchaseOrderNumber);

            // Define the entity set AFTER registering the type
            builder.EntitySet<ProjectEntity>("Projects");

            builder.EntitySet<UserEntity>("Users");

            // Explicitly set namespace and container
            builder.Namespace = "FourSPM_WebService.Data.OData.FourSPM";
            builder.ContainerName = "FourSPMContainer";

            return builder.GetEdmModel();
        }
    }

}
