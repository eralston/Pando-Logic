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
    /// An organization using Pando Logic to track its progress
    /// </summary>
    public class Company
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        // Required Field
        [Required]
        public string Name { get; set; }

        [Display(Name = "Number of Employees")]
        public int NumberOfEmployees { get; set; }

        // To-One on Industry
        [ForeignKey("Industry")]
        public int IndustryId { get; set; }
        public virtual Industry Industry { get; set; }

        // To-Many on PhoneNumber
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; }

        // TODO: Company Image using Azure Blob Storage!
    }
}