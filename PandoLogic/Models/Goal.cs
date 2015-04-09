using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Security;

using Masticore;
using System.ComponentModel;

namespace PandoLogic.Models
{
    /// <summary>
    /// A unit of high-level planning for an organization
    /// </summary>
    public class Goal : BaseModel, ICommentable, IOptionalCompanyOwnedModel, IUserOwnedModel
    {
        /// <summary>
        /// List of configurable colors for goals
        /// </summary>
        public enum GoalColor
        {
            [Display(Name = "Light Blue")]
            LightBlue,
            Aqua,
            Blue,
            Green,
            Yellow,
            Purple,
            Navy,
            Teal,
            Orange,
        }

        /// <summary>
        /// A mapping of GoalColors into CSS classes
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ClassForColor(GoalColor color)
        {
            switch (color)
            {
                case GoalColor.Aqua:
                    return "bg-aqua";
                case GoalColor.Blue:
                    return "bg-blue";
                case GoalColor.Green:
                    return "bg-green";
                case GoalColor.Yellow:
                    return "bg-yellow";
                case GoalColor.Purple:
                    return "bg-purple";
                case GoalColor.Navy:
                    return "bg-navy";
                case GoalColor.Teal:
                    return "bg-teal";
                case GoalColor.Orange:
                    return "bg-orange";
                case GoalColor.LightBlue:
                    return "bg-light-blue";
                default:
                    return "bg-aqua";
            }
        }

        /// <summary>
        /// List of icons for goals, shown in summary boxes
        /// </summary>
        public enum GoalIcon
        {
            Bars, // fa-bar-chart-o
            Calendar, // fa-calendar-o 
            Dollar, // fa-usd
            Chart, // fa-signal
            Key, // fa-key
            Strategy, // fa-puzzle-piece
            Task, // fa-check
            Goal // fa-tasks
        }

        public static string ClassForIcon(GoalIcon icon)
        {
            switch (icon)
            {
                case GoalIcon.Bars:
                    return "fa-bar-chart-o";
                case GoalIcon.Calendar:
                    return "fa-calendar-o";
                case GoalIcon.Dollar:
                    return "fa-usd";
                case GoalIcon.Chart:
                    return "fa-signal";
                case GoalIcon.Key:
                    return "fa-key";
                case GoalIcon.Strategy:
                    return "fa-puzzle-piece";
                case GoalIcon.Task:
                    return "fa-check";
                case GoalIcon.Goal:
                    return "fa-tasks";
                default:
                    return "fa-tasks";
            }
        }

        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        /// <summary>
        /// Gets or sets the start date for a goal
        /// NOTE: This should be translated to UTC from localized user input
        /// </summary>
        [Display(Name = "Start Date")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the targeted completion datetime for a goal
        /// NOTE: This should be translated to UTC from localized user input
        /// </summary>
        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDateUtc { get; set; }

        [Required]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // To-Many on WorkItems
        public virtual ICollection<WorkItem> WorkItems { get; set; }

        [Display(Name = "Archive Date")]
        public DateTime? ArchiveDateUtc { get; set; }

        public bool IsTemplate { get; set; }

        public GoalColor Color { get; set; }

        public GoalIcon Icon { get; set; }

        #region NotMapped Properties

        [NotMapped]
        public int Ordinal { get; set; }

        [NotMapped]
        public string CommentControllerName { get { return "Goals"; } }

        [NotMapped]
        public string CommentActionName { get { return "Details"; } }

        #endregion

        #region Derived Properties

        [NotMapped]
        public string ColorClass
        {
            get
            {
                if (this.ArchiveDateUtc.HasValue)
                    return "bg-gray";

                if (this.WorkItems.Count > 0 && this.CompletedWorkItemCount() == this.WorkItems.Count)
                    return "bg-green";

                if (this.DueDateUtc.HasValue && this.DueDateUtc.Value < DateTime.Now)
                    return "bg-red";

                return ClassForColor(Color);
            }
        }

        [NotMapped]
        public string IconClass
        {
            get
            {
                return ClassForIcon(Icon);
            }
        }

        #endregion

        #region Methods

        string _progress = null;

        public string CalculateProgress()
        {
            if (!string.IsNullOrEmpty(_progress))
            {
                return _progress;
            }

            if (this.ArchiveDateUtc.HasValue)
            {
                _progress = "100";
                return _progress;
            }

            int complete = CompletedWorkItemCount();
            int count = this.WorkItems.Count;

            if(count == 0)
            {
                _progress = "0";
                return _progress;
            }

            float retFloat = (float)complete / (float)count;
            retFloat = retFloat * 100f;
            int retInt = (int)retFloat;

            _progress = retInt.ToString();
            return _progress;
        }

        public int CompletedWorkItemCount()
        {
            int complete = 0;
            foreach (WorkItem item in this.WorkItems)
            {
                if (item.CompletedDateUtc.HasValue)
                    ++complete;
            }
            return complete;
        }

        /// <summary>
        /// Archives this goal, also setting all unfinished tasks as completed with the same date
        /// </summary>
        public void Archive()
        {
            DateTime archiveDate = DateTime.UtcNow;

            ArchiveDateUtc = archiveDate;

            foreach (WorkItem task in WorkItems)
            {
                if (!task.CompletedDateUtc.HasValue)
                {
                    task.CompletedDateUtc = archiveDate;
                }
            }
        }

        /// <summary>
        /// Undoes the archive of this task, also rendering the closed tasks incomplete
        /// </summary>
        public void UndoArchive()
        {
            DateTime? archiveDate = this.ArchiveDateUtc;

            if (!archiveDate.HasValue)
                return;

            this.ArchiveDateUtc = null;

            foreach (WorkItem task in WorkItems)
            {
                if (task.CompletedDateUtc.HasValue && task.CompletedDateUtc.Value == archiveDate.Value)
                {
                    task.CompletedDateUtc = null;
                }
            }
        }

        #endregion
    }

    public static class GoalExtensions
    {
        /// <summary>
        /// Returns all goals corresponding to that member
        /// </summary>
        /// <param name="goals"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static IQueryable<Goal> WhereMember(this DbSet<Goal> goals, Member member)
        {
            int companyId = member.CompanyId;
            return goals.Where(g => g.CompanyId == companyId);
        }

        /// <summary>
        /// Returns all active goals for the given company
        /// </summary>
        /// <param name="goals"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static IQueryable<Goal> WhereActiveGoalForCompany(this DbSet<Goal> goals, int companyId)
        {
            return goals.Where(g => g.ArchiveDateUtc == null && g.IsTemplate == false && g.CompanyId == companyId)
                .Include(g => g.WorkItems)
                .OrderBy(g => g.DueDateUtc);
        }

        public static Goal Create(this DbSet<Goal> goals, int? companyId, string userId)
        {
            Goal goal = goals.Create();

            goal.CreatedDateUtc = DateTime.UtcNow;
            goal.StartDateUtc = goal.CreatedDateUtc;
            goal.CompanyId = companyId;
            goal.UserId = userId;

            goal.WorkItems = new List<WorkItem>();

            goals.Add(goal);

            return goal;
        }

        public static Goal CreateFromTemplate(this DbSet<Goal> goals, DbSet<WorkItem> tasks, Goal template, int companyId, string userId)
        {
            Goal newGoal = goals.Create(companyId, userId);

            newGoal.Title = template.Title;
            newGoal.Description = template.Description;
            newGoal.IsTemplate = false;

            foreach (WorkItem taskTemplate in template.WorkItems)
            {
                WorkItem newTask = tasks.CreateFromTemplate(taskTemplate, companyId, userId);
                newTask.Goal = newGoal;
                newGoal.WorkItems.Add(newTask);
            }

            return newGoal;
        }
    }
}