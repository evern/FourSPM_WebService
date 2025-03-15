using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Models.Session;
using FourSPM_WebService.Data.OData.FourSPM;
using Microsoft.EntityFrameworkCore;

namespace FourSPM_WebService.Data.Queries
{
    public class ProjectQueries
    {
        public static IQueryable<ProjectEntity> UserProjectQuery(FourSPMContext context, ApplicationUser user)
        {
            // For now, return all non-deleted projects
            return context.PROJECTs
                .Where(p => !p.DELETED.HasValue)
                .OrderByDescending(p => p.CREATED)
                .Select(p => new ProjectEntity
                {
                    Guid = p.GUID,
                    ClientGuid = p.GUID_CLIENT,
                    ProjectNumber = p.PROJECT_NUMBER,
                    Name = p.NAME,
                    PurchaseOrderNumber = p.PURCHASE_ORDER_NUMBER,
                    ProjectStatus = p.PROJECT_STATUS,
                    ProgressStart = p.PROGRESS_START,
                    Created = p.CREATED,
                    CreatedBy = p.CREATEDBY,
                    Updated = p.UPDATED,
                    UpdatedBy = p.UPDATEDBY,
                    Deleted = p.DELETED,
                    DeletedBy = p.DELETEDBY,
                    // Include client data if available
                    Client = p.Client != null ? new ClientEntity
                    {
                        Guid = p.Client.GUID,
                        Number = p.Client.NUMBER,
                        Description = p.Client.DESCRIPTION,
                        ClientContactName = p.Client.CLIENT_CONTACT_NAME,
                        ClientContactNumber = p.Client.CLIENT_CONTACT_NUMBER,
                        ClientContactEmail = p.Client.CLIENT_CONTACT_EMAIL
                    } : null
                });
        }
    }
}
