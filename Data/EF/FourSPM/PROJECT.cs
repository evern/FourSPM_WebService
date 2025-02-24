using System;
using System.Collections.Generic;
using FourSPM_WebService.Models.Shared.Enums;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class PROJECT
{
    public Guid GUID { get; set; }

    public string CLIENT_NUMBER { get; set; } = null!;

    public string PROJECT_NUMBER { get; set; } = null!;

    public string? CLIENT_CONTACT { get; set; }

    public string? PURCHASE_ORDER_NUMBER { get; set; }

    public ProjectStatus PROJECT_STATUS { get; set; }

    public string? NAME { get; set; }

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }

    public virtual ICollection<DELIVERABLE> Deliverables { get; set; } = new List<DELIVERABLE>();
}
