namespace FourSPM_WebService.Models.Session
{
    public enum Permission
    {
        None,
        All,
        ReadOnly
    }

    public class RolePermissionModel
    {
        public string Name { get; set; } = null!;
        public Permission Permission { get; set; }
    }
}
