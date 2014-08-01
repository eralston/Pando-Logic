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

        #region Methods

        public string CalculateProgress()
        {
            int complete = CompletedWorkItemCount();
            int count = this.WorkItems.Count;

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
    }
}