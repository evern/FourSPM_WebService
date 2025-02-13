using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Session;

namespace FourSPM_WebService.Data.Queries
{
    public class ProjectQueries
    {
        public static IQueryable<PROJECT> UserProjectQuery(FourSPMContext context, ApplicationUser user)
        {
            var userId = user.UserId ?? Guid.NewGuid();
;
            var activeProjects = context.PROJECTs
                .Where(p => !p.DELETED.HasValue);

            return activeProjects;
        }
    }
}
