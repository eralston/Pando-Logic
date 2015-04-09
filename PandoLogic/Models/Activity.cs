using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

using Masticore;
using Microsoft.WindowsAzure.Storage.Table;

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
    public class Activity : TableEntity, IUserOwnedModel
    {
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public Activity() { }

        /// <summary>
        /// Creation constructor
        /// </summary>
        /// <param name="authorUserId"></param>
        /// <param name="title"></param>
        public Activity(string authorUserId, string title)
        {
            this.UserId = authorUserId;
            this.Title = title;
            GenerateRowKey();
        }

        /// <summary>
        /// Generates and applies the rowkey
        /// </summary>
        public void GenerateRowKey()
        {
            this.RowKey = TableStorageManager.GenerateTicksDescendingRowKey();
        }

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

        /// <summary>
        /// Sets the title of this activity, using the mult-part format with link URL
        /// </summary>
        /// <param name="linkText"></param>
        /// <param name="linkUrl"></param>
        /// <param name="title"></param>
        public void SetTitle(string linkText, string linkUrl)
        {
            string preview = string.Format("<a href='{0}'>{1}</a>", linkUrl, linkText);
            this.Title = preview;
        }

        [AllowHtml]
        [Required]
        public virtual string Title { get; set; }

        [AllowHtml]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [IgnoreProperty]
        public ActivityType Type { get; set; }

        public int TypeId 
        {
            get
            {
                return (int)Type;
            }
            set
            {
                Type = (ActivityType)value;
            }
        }

        [DefaultValue(true)]
        public bool IsAbleToBeEdited { get; set; }

        [DefaultValue(true)]
        public bool IsAbleToBeDeleted { get; set; }

        [IgnoreProperty]
        public string IconClass
        {
            get { return Activity.ClassesForActivityType(this.Type); }
        }

        // To-One on User Who Originated this action
        public string UserId { get; set; }

        public int CompanyId { get; set; }
    }

}