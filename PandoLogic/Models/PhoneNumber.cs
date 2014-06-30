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
    /// Enumerates the various types of phone numbers that people or companies may have in the system
    /// </summary>
    public enum PhoneNumberType
    {
        Mobile,
        Office,
        Home,
        Other
    }

    /// <summary>
    /// Model for tracking a phone number (for an individual or company)
    /// </summary>
    public class PhoneNumber
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = "Phone Number")]
        [Required]
        public string Number { get; set; }

        public string Extension { get; set; }

        public PhoneNumberType Type { get; set; }
    }
}