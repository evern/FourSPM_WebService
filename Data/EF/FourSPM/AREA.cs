using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class AREA
{
    [Key]
    public Guid GUID { get; set; }

    public Guid GUID_PROJECT { get; set; }

    public string NUMBER { get; set; } = null!;

    public string DESCRIPTION { get; set; } = null!;

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }

    public virtual PROJECT Project { get; set; } = null!;
}
