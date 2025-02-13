using System;
using System.Collections.Generic;

namespace FourSPM_WebService.EF.FourSPM;

public partial class USER
{
    public Guid GUID { get; set; }

    public string? FIRST_NAME { get; set; }

    public string? LAST_NAME { get; set; }

    public DateTime CREATED { get; set; }

    public Guid CREATEDBY { get; set; }

    public DateTime? UPDATED { get; set; }

    public Guid? UPDATEDBY { get; set; }

    public DateTime? DELETED { get; set; }

    public Guid? DELETEDBY { get; set; }
}
