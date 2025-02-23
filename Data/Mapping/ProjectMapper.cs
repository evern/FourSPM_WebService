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
                ClientNumber = project.CLIENT_NUMBER,
                ProjectNumber = project.PROJECT_NUMBER,
                Name = project.NAME,
                ClientContact = project.CLIENT_CONTACT,
                PurchaseOrderNumber = project.PURCHASE_ORDER_NUMBER,
                ProjectStatus = project.PROJECT_STATUS,
                Created = project.CREATED,
                CreatedBy = project.CREATEDBY,
                Updated = project.UPDATED,
                UpdatedBy = project.UPDATEDBY,
                Deleted = project.DELETED,
                DeletedBy = project.DELETEDBY
            };
        }
    }
}
