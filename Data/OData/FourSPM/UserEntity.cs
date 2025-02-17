using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FourSPM_WebService.Data.OData.FourSPM;

namespace FourSPM_WebService.Data.OData.FourSPM
{
    public class UserEntity
    {
        [Key] // Required for OData
        public Guid Guid { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public required string UserName { get; set; }

        public required string Password { get; set; }

        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Updated { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        public string FullName => string.Concat(FirstName, " ", LastName);
    }
}
