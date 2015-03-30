using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Enumerates the various types of addresses individuals and companies may enter in the system
    /// </summary>
    public enum AddressType
    {
        Business,
        Home,
        Other
    }

    public class Address : BaseModel
    {
        [Display(Name = "Updated Date")]
        public DateTime? LastUpdateUtc { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address1 { get; set; }

        [Display(Name = "Address (Optional)")]
        public string Address2 { get; set; }

        public string City { get; set; }

        [Display(Name = "State or Province")]
        public string StateOrProvince { get; set; }

        public string Country { get; set; }

        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        public AddressType Type { get; set; }
    }
}