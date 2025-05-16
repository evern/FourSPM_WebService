using System;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public partial class ROLE_PERMISSION
    {
        public int GUID { get; set; }
        public int GUID_ROLE { get; set; }
        public string PERMISSION { get; set; } = null!;
        public DateTime? CREATED { get; set; }
        public string? CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public string? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public string? DELETEDBY { get; set; }

        // Navigation property
        public virtual ROLE ROLE { get; set; } = null!;
    }
}
