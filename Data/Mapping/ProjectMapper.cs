using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Data.Mapping
{
    public static class ProjectMapper
    {
        public static ProjectEntity ToEntity(PROJECT project)
        {
            return new ProjectEntity
            {
                Guid = project.GUID,
                ClientGuid = project.GUID_CLIENT,
                ProjectNumber = project.PROJECT_NUMBER,
                Name = project.NAME,
                PurchaseOrderNumber = project.PURCHASE_ORDER_NUMBER,
                ProjectStatus = project.PROJECT_STATUS,
                ProgressStart = project.PROGRESS_START,
                Created = project.CREATED,
                CreatedBy = project.CREATEDBY,
                Updated = project.UPDATED,
                UpdatedBy = project.UPDATEDBY,
                Deleted = project.DELETED,
                DeletedBy = project.DELETEDBY,
                // Include client data if available
                Client = project.Client != null ? new ClientEntity
                {
                    Guid = project.Client.GUID,
                    Number = project.Client.NUMBER,
                    Description = project.Client.DESCRIPTION,
                    ClientContactName = project.Client.CLIENT_CONTACT_NAME,
                    ClientContactNumber = project.Client.CLIENT_CONTACT_NUMBER,
                    ClientContactEmail = project.Client.CLIENT_CONTACT_EMAIL
                } : null
            };
        }
    }
}
