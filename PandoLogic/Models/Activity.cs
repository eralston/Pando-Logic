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
    /// An enumeration of the various type of activities, used to categorize them in the UI
    /// </summary>
    public enum ActivityType
    {
        Message,            // fa-envelope bg-blue      EG: Private Message from a User
        Comment,            // fa-comments bg-yellow    EG: Comment on Assigned Task, etc
        WorkAdded,          // fa-plus bg-aqua
        WorkCompleted,      // fa-check bg-green
        WorkDeleted,        // fa-times bg-red
        UserAction          // fa-user bg-purple        EG: Register, Accept Invite Into System, Assigning Task, etc
    }

    /// <summary>
    /// A model for saving a historic record of actions in the system, in particular to review actions of a team
    /// </summary>
    public class Activity
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public ActivityType ActivityType { get; set; }

        // To-One on User Who Originated this action
        // This is optional, since some activities are done by the system
        [ForeignKey("Author")]
        public string AuthorId { get; set; }
        public virtual ApplicationUser Author { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }
    }
}