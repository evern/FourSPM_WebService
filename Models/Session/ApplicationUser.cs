using Microsoft.OData.ModelBuilder.Core.V1;

namespace FourSPM_WebService.Models.Session
{
    public class ApplicationUser
    {
        public string Upn { get; set; }
        public string? UserName { get; set; }
        public Guid? UserId { get; set; }
        public IReadOnlyCollection<RolePermissionModel> Permissions { get; set; } = new List<RolePermissionModel>();

        public bool HasAccessPermissions(string item, Permission permission)
        {
            return Permissions.Any(x => x.Permission == permission && x.Name == item);
        }
    }
}
