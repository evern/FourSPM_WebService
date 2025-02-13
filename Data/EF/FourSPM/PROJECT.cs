using System;
using System.Collections.Generic;

namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class PROJECT
{
    public Guid GUID { get; set; }

    public string NUMBER { get; set; } = null!;

    public string? NAME { get; set; }

    public string? CLIENT { get; set; }

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }
}
