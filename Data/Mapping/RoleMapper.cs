using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Data.Mapping
{
    public static class RoleMapper
    {
        public static RoleEntity ToEntity(ROLE role)
        {
            if (role == null) return null!;
            
            return new RoleEntity
            {
                Guid = role.GUID,
                Name = role.NAME,
                DisplayName = role.DISPLAY_NAME,
                Description = role.DESCRIPTION,
                IsSystemRole = role.IS_SYSTEM_ROLE,
                Created = role.CREATED,
                CreatedBy = role.CREATEDBY,
                Updated = role.UPDATED,
                UpdatedBy = role.UPDATEDBY,
                Deleted = role.DELETED,
                DeletedBy = role.DELETEDBY
            };
        }
        
        public static ROLE ToModel(RoleEntity entity)
        {
            if (entity == null) return null!;
            
            return new ROLE
            {
                GUID = entity.Guid,
                NAME = entity.Name,
                DISPLAY_NAME = entity.DisplayName,
                DESCRIPTION = entity.Description,
                IS_SYSTEM_ROLE = entity.IsSystemRole,
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
