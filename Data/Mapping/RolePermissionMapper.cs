using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Data.Mapping
{
    public static class RolePermissionMapper
    {
        public static RolePermissionEntity ToEntity(ROLE_PERMISSION rolePermission)
        {
            if (rolePermission == null) return null!;
            
            return new RolePermissionEntity
            {
                Guid = rolePermission.GUID,
                RoleGuid = rolePermission.GUID_ROLE,
                Permission = rolePermission.PERMISSION,
                Created = rolePermission.CREATED,
                CreatedBy = rolePermission.CREATEDBY,
                Updated = rolePermission.UPDATED,
                UpdatedBy = rolePermission.UPDATEDBY,
                Deleted = rolePermission.DELETED,
                DeletedBy = rolePermission.DELETEDBY
            };
        }
        
        public static ROLE_PERMISSION ToModel(RolePermissionEntity entity)
        {
            if (entity == null) return null!;
            
            return new ROLE_PERMISSION
            {
                GUID = entity.Guid,
                GUID_ROLE = entity.RoleGuid,
                PERMISSION = entity.Permission,
                CREATED = entity.Created,
                CREATEDBY = entity.CreatedBy,
                UPDATED = entity.Updated,
                UPDATEDBY = entity.UpdatedBy,
                DELETED = entity.Deleted,
                DELETEDBY = entity.DeletedBy
            };
        }
    }
}
