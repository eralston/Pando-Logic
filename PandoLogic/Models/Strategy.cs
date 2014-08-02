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
    /// 
    /// </summary>
    public class Strategy
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [Required]
        public virtual string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // To-One on User Who Originated this action
        // This is optional, since some activities are done by the system
        [ForeignKey("Author")]
        public string AuthorId { get; set; }
        public virtual ApplicationUser Author { get; set; }

        // To-Many on StrategyPhase
        public virtual ICollection<StrategyPhase> Phases { get; set; }
    }
}