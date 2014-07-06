using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Security;

namespace PandoLogic.Models
{
    /// <summary>
    /// Enumerates the various types of addresses individuals and companies may enter in the system
    /// </summary>
    public enum AddressType
    {
        Office,
        Home,
        Other
    }

    public class Address
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? LastUpdate { get; set; }

        [Required]
        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string City { get; set; }

        public string StateOrProvince { get; set; }

        public string Country { get; set; }

        public string PostalCode { get; set; }

        public AddressType Type { get; set; }
    }
}