using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;

namespace PandoLogic.Models
{
    /// <summary>
    /// These are called "Tasks" in the UIs
    /// A single unit of work, usually under a goal
    /// </summary>
    public class WorkItem : BaseModel, ICommentable
    {
        // To-One on ApplicationUser
        [ForeignKey("Creator")]
        public string CreatorId { get; set; }
        public virtual ApplicationUser Creator { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on Goal
        [ForeignKey("Goal")]
        public int? GoalId { get; set; }
        public virtual Goal Goal { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("Assignee")]
        public string AssigneeId { get; set; }
        public virtual ApplicationUser Assignee { get; set; }

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

        [Display(Name = "Estimated Hours")]
        public float? EstimatedTime { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        public bool IsTemplate { get; set; }

        #region ICommentable

        // To-Many on Activity
        public virtual ICollection<Activity> Comments { get; set; }

        [NotMapped]
        public string CommentControllerName { get { return "Tasks"; } }

        [NotMapped]
        public string CommentActionName { get { return "Details"; } }

        #endregion
    }

    public static class WorkItemExtensions
    {
        public static IQueryable<WorkItem> WhereCompany(this DbSet<WorkItem> workItems, int companyId)
        {
            return workItems.Where(w => w.CompanyId == companyId).Include(w => w.Assignee).Include(w => w.Creator);
        }

        public static IQueryable<WorkItem> WhereCompanyNonGoal(this DbSet<WorkItem> workItems, int companyId)
        {
            return workItems.Where(w => w.CompanyId == companyId && w.GoalId == null).Include(w => w.Assignee).Include(w => w.Creator);
        }

        public static IQueryable<WorkItem> WhereGoal(this DbSet<WorkItem> workItems, int goalId)
        {
            return workItems.Where(w => w.GoalId == goalId).Include(w => w.Assignee).Include(w => w.Creator);
        }

        public static IQueryable<WorkItem> WhereAssignedUser(this DbSet<WorkItem> workItems, string userId)
        {
            return workItems.Where(w => w.AssigneeId == userId && w.Company.IsSoftDeleted == false);
        }

        public static async Task RemoveWorkItemsForGoal(this ApplicationDbContext context, int goalId)
        {
            WorkItem[] workItems = await context.WorkItems.WhereGoal(goalId).ToArrayAsync();
            foreach (WorkItem item in workItems)
            {
                context.WorkItems.Remove(item);
            }
        }

        public static WorkItem Create(this DbSet<WorkItem> tasks, int companyId, string userId)
        {
            WorkItem task = tasks.Create();

            task.CreatedDate = DateTime.UtcNow;
            task.CreatorId = userId;
            task.CompanyId = companyId;

            tasks.Add(task);

            return task;
        }

        public static WorkItem CreateFromTemplate(this DbSet<WorkItem> tasks, WorkItem template, int companyId, string userId)
        {
            WorkItem task = tasks.Create(companyId, userId);

            task.Title = template.Title;
            task.Description = template.Description;
            task.EstimatedTime = template.EstimatedTime;

            return task;
        }

        public static IQueryable<WorkItem> WhereAssignedUserAndCompany(this DbSet<WorkItem> tasks, string userId, int companyId)
        {
            return tasks.Where(t => t.AssigneeId == userId && t.CompanyId == companyId);
        }
    }
}