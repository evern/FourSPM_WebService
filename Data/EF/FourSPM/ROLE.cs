using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.EF.FourSPM
{
    public partial class ROLE
    {
        public ROLE()
        {
            ROLE_PERMISSIONs = new HashSet<ROLE_PERMISSION>();
        }

        public int GUID { get; set; }
        public string NAME { get; set; } = null!;
        public string DISPLAY_NAME { get; set; } = null!;
        public string? DESCRIPTION { get; set; }
        public bool IS_SYSTEM_ROLE { get; set; }
        public DateTime? CREATED { get; set; }
        public string? CREATEDBY { get; set; }
        public DateTime? UPDATED { get; set; }
        public string? UPDATEDBY { get; set; }
        public DateTime? DELETED { get; set; }
        public string? DELETEDBY { get; set; }

        // Navigation property
        public virtual ICollection<ROLE_PERMISSION> ROLE_PERMISSIONs { get; set; }
    }
}
