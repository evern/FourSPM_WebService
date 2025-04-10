using System;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class VARIATION
{
    [Key]
    public Guid GUID { get; set; }

    public Guid GUID_PROJECT { get; set; }

    public string NAME { get; set; } = null!;

    public string? COMMENTS { get; set; }

    public DateTime? SUBMITTED { get; set; }

    public Guid? SUBMITTEDBY { get; set; }

    public DateTime? CLIENT_APPROVED { get; set; }

    public Guid? CLIENT_APPROVEDBY { get; set; }

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }

    public virtual PROJECT Project { get; set; } = null!;
    
    public virtual ICollection<DELIVERABLE> Deliverables { get; set; } = new List<DELIVERABLE>();
}
