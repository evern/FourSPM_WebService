using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class CLIENT
{
    [Key]
    public Guid GUID { get; set; }

    public string NUMBER { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    public string? CLIENT_CONTACT_NAME { get; set; }
    
    public string? CLIENT_CONTACT_NUMBER { get; set; }
    
    public string? CLIENT_CONTACT_EMAIL { get; set; }

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }

    public virtual ICollection<PROJECT> Projects { get; set; } = new List<PROJECT>();
}
