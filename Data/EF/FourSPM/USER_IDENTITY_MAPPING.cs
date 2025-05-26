using System;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public class USER_IDENTITY_MAPPING
    {
        public Guid GUID { get; set; }
        public string USERNAME { get; set; } = string.Empty;
        public string EMAIL { get; set; } = string.Empty;
        public DateTime CREATED { get; set; }
        public DateTime? LAST_LOGIN { get; set; }
        public DateTime? DELETED { get; set; }
        public Guid? DELETEDBY { get; set; }
    }
}
