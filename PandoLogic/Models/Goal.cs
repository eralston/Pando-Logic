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
    /// A unit of high-level planning for an organization
    /// </summary>
    public class Goal : PandoLogic.Models.ICommentable
    {
        /// <summary>
        /// List of configurable colors for goals
        /// </summary>
        public enum GoalColor
        {
            Aqua,
            Blue,
            Green,
            Yellow,
            Purple
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

        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDate { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("Creator")]
        public string CreatorId { get; set; }
        public virtual ApplicationUser Creator { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Required]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // To-Many on WorkItems
        public virtual ICollection<WorkItem> WorkItems { get; set; }

        // To-Many on Activity
        public virtual ICollection<Activity> Comments { get; set; }

        [Display(Name = "Archive Date")]
        public DateTime? ArchiveDate { get; set; }

        public bool IsTemplate { get; set; }

        public GoalColor Color { get; set; }

        public GoalIcon Icon { get; set; }

        #region NotMapped Properties

        [NotMapped]
        public int Ordinal { get; set; }

        #endregion

        #region Derived Properties

        [NotMapped]
        public string ColorClass
        {
            get
            {
                switch (Color)
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
                    default:
                        return "bg-aqua";
                }
            }
        }

        [NotMapped]
        public string IconClass
        {
            get
            {
                switch (Icon)
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
        }

        #endregion

        #region Methods

        public string CalculateProgress()
        {
            if (this.ArchiveDate.HasValue)
                return "100";

            int complete = CompletedWorkItemCount();
            int count = this.WorkItems.Count;

            if (count == 0)
                return "0";

            float retFloat = (float)complete / (float)count;
            retFloat = retFloat * 100f;
            int retInt = (int)retFloat;
            string ret = retInt.ToString();
            return ret;
        }

        public int CompletedWorkItemCount()
        {
            int complete = 0;
            foreach (WorkItem item in this.WorkItems)
            {
                if (item.CompletedDate.HasValue)
                    ++complete;
            }
            return complete;
        }

        /// <summary>
        /// Archives this goal, also setting all unfinished tasks as completed with the same date
        /// </summary>
        public void Archive()
        {
            DateTime archiveDate = DateTime.Now;

            ArchiveDate = archiveDate;

            foreach (WorkItem task in WorkItems)
            {
                if (!task.CompletedDate.HasValue)
                {
                    task.CompletedDate = archiveDate;
                }
            }
        }

        /// <summary>
        /// Undoes the archive of this task, also rendering the closed tasks incomplete
        /// </summary>
        public void UndoArchive()
        {
            DateTime? archiveDate = this.ArchiveDate;

            if (!archiveDate.HasValue)
                return;

            this.ArchiveDate = null;

            foreach (WorkItem task in WorkItems)
            {
                if (task.CompletedDate.HasValue && task.CompletedDate.Value == archiveDate.Value)
                {
                    task.CompletedDate = null;
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

        public static Goal Create(this DbSet<Goal> goals, int companyId, string userId)
        {
            Goal goal = goals.Create();

            goal.CreatedDate = DateTime.Now;
            goal.CompanyId = companyId;
            goal.CreatorId = userId;

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