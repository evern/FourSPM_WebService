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

            return builder.GetEdmModel();
        }
    }
}
