using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FourSPM_WebService.Models.Shared.Enums;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class PROJECT
{
    [Key]
    public Guid GUID { get; set; }

    public Guid? GUID_CLIENT { get; set; }

    public string PROJECT_NUMBER { get; set; } = null!;

    public string? NAME { get; set; }

    public string? PURCHASE_ORDER_NUMBER { get; set; }

    public ProjectStatus PROJECT_STATUS { get; set; }

    public DateTime? PROGRESS_START { get; set; }
    
    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }
    
    public string? CONTACT_NAME { get; set; }

    public string? CONTACT_NUMBER { get; set; }

    public string? CONTACT_EMAIL { get; set; }

    public virtual CLIENT? Client { get; set; }

    public virtual ICollection<DELIVERABLE> Deliverables { get; set; } = new List<DELIVERABLE>();

    public virtual ICollection<AREA> Areas { get; set; } = new List<AREA>();

    public virtual ICollection<VARIATION> Variations { get; set; } = new List<VARIATION>();
}
