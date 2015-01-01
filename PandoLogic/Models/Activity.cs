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
        WorkArchived,       // fa-lock bg-green
        WorkUndoArchived,   // fa-unlock-alt bg-yellow
        UserAction,         // fa-user bg-purple        EG: Register, Accept Invite Into System, Assigning Task, etc
        TeamNotification    // fa-users bg-yellow
    }

    /// <summary>
    /// A model for saving a historic record of actions in the system, in particular to review actions of a team
    /// </summary>
    public class Activity : BaseModel
    {
        /// <summary>
        /// Returns the class names for the given activity type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ClassesForActivityType(ActivityType type)
        {
            string ret = "";

            switch (type)
            {
                case ActivityType.Message:
                    ret = "fa-envelope bg-blue";
                    break;
                case ActivityType.Comment:
                    ret = "fa-comments bg-yellow";
                    break;
                case ActivityType.WorkAdded:
                    ret = "fa-plus bg-aqua";
                    break;
                case ActivityType.WorkCompleted:
                    ret = "fa-check bg-green";
                    break;
                case ActivityType.WorkDeleted:
                    ret = "fa-times bg-red";
                    break;
                case ActivityType.WorkArchived:
                    ret = "fa-lock bg-green";
                    break;
                case ActivityType.WorkUndoArchived:
                    ret = "fa-unlock-alt bg-yellow";
                    break;
                case ActivityType.UserAction:
                    ret = "fa-user bg-purple";
                    break;
                case ActivityType.TeamNotification:
                    ret = "fa-users bg-yellow";
                    break;
            }

            return ret;
        }

        [Required]
        public virtual string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public ActivityType Type { get; set; }

        [NotMapped]
        public string IconClass
        {
            get { return Activity.ClassesForActivityType(this.Type); }
        }

        // To-One on User Who Originated this action
        // This is optional, since some activities are done by the system
        [ForeignKey("Author")]
        public string AuthorId { get; set; }
        public virtual ApplicationUser Author { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public int? GoalId { get; set; }
        public int? WorkItemId { get; set; }
    }

    public static class ActivityExtensions
    {

        public static Activity Create(this DbSet<Activity> activities, string authorUserId, string title)
        {
            Activity activity = activities.Create();

            activity.CreatedDate = DateTime.Now;
            activity.AuthorId = authorUserId;
            activity.Company = null;
            activity.Title = title;

            activities.Add(activity);

            return activity;
        }

        public static Activity Create(this DbSet<Activity> activities, string authorUserId, Company company, string title)
        {
            Activity activity = activities.Create();

            activity.CreatedDate = DateTime.Now;
            activity.AuthorId = authorUserId;
            activity.Company = company;
            activity.Title = title;

            activities.Add(activity);

            return activity;
        }

        public static IOrderedQueryable<Activity> WhereCompanyOrAuthor(this DbSet<Activity> activities, int? companyId, string authorId)
        {
            return activities
                        .Include(a => a.Author)
                        .Include(a => a.Company)
                        .Where(a => a.CompanyId == companyId || a.AuthorId == authorId)
                        .OrderByDescending(a => a.CreatedDate);
        }
    }
}