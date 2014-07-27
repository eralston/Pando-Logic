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
    public class WorkItem : ICommentable
    {
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
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on Goal
        [ForeignKey("Goal")]
        public int? GoalId { get; set; }
        public virtual Goal Goal { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("Assignee")]
        public string AssigneeId { get; set; }
        public virtual ApplicationUser Assignee { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }

        [Required]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // To-Many on Activity
        public virtual ICollection<Activity> Comments { get; set; }
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
            return workItems.Where(w => w.AssigneeId == userId);
        }

        public static async Task RemoveWorkItemsForGoal(this ApplicationDbContext context, int goalId)
        {
            WorkItem[] workItems = await context.WorkItems.WhereGoal(goalId).ToArrayAsync();
            foreach(WorkItem item in workItems)
            {
                context.WorkItems.Remove(item);
            }
        }
    }
}