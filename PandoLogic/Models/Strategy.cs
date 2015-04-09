using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Enumerates the options for intervals of a strategy
    /// </summary>
    public enum StrategyInterval
    {
        None,
        Days,
        Weeks,
        Months
    }

    /// <summary>
    /// Modelo for repeatable collections of goals with related tasks underneath them
    /// </summary>
    public class Strategy : BaseModel, ICommentable, IUserOwnedModel
    {
        [Required]
        [MaxLength(100)]
        public virtual string Title { get; set; }

        [Required]
        [MaxLength(200)]
        public string Summary { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // To-One on User Who Originated this action
        // This is optional, since some activities are done by the system
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Display(Name = "Goal Interval")]
        public StrategyInterval Interval { get; set; }

        // To-Many on StrategyGoal
        public virtual ICollection<StrategyGoal> Goals { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Community Rating")]
        public float Rating { get; set; }

        public string SearchText { get; set; }

        public virtual ICollection<StrategyRating> Ratings { get; set; }

        public virtual ICollection<StrategyAdoption> Adoptions { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        #region Methods

        public void UpdateSearchText()
        {
            SearchText = string.Format("{0} {1} {2}", Title.ToUpper(), Summary.ToUpper(), Description.ToUpper());
        }

        #endregion

        #region Methods For Managing Child Goals

        /// <summary>
        /// Adds a new copy of the given goal to this strategy's collection
        /// </summary>
        /// <param name="existingGoal"></param>
        public void AddCopyOfGoalAsTemplate(Goal existingGoal)
        {
            // Make a new goal and link to strategy
            StrategyGoal newStrategyGoal = new StrategyGoal();
            Goal newGoalTemplate = new Goal();

            // Link the objects to this strategy 
            newStrategyGoal.Strategy = this;
            newStrategyGoal.Goal = newGoalTemplate;
            newStrategyGoal.CreatedDateUtc = DateTime.UtcNow;

            // Set the fixed fields
            newGoalTemplate.CreatedDateUtc = newStrategyGoal.CreatedDateUtc;
            newGoalTemplate.IsTemplate = true;

            // Map the existing goal into the new goal
            newGoalTemplate.CompanyId = existingGoal.CompanyId;
            newGoalTemplate.UserId = existingGoal.UserId;
            newGoalTemplate.Title = existingGoal.Title;
            newGoalTemplate.Description = existingGoal.Description;

            if (Goals == null)
            {
                Goals = new List<StrategyGoal>();
            }
            Goals.Add(newStrategyGoal);
        }

        #endregion

        #region Methods

        public int ShiftForDayOfWeek(DayOfWeek day)
        {
            return (8 - (int)day) % 7;
        }

        public DateTime GetFirstStartDateForIntervalFromNow()
        {
            DateTime now = DateTime.UtcNow;

            if (Interval == StrategyInterval.None)
                return now;

            if (Interval == StrategyInterval.Days)
            {
                now = now.AddDays(1);
                return now;
            }

            if (Interval == StrategyInterval.Weeks)
            {
                // Find the next monday
                now = now.AddDays(ShiftForDayOfWeek(now.DayOfWeek));
                return now;
            }

            if (Interval == StrategyInterval.Months)
            {
                // Find the next start of the month
                while (now.Day != 1)
                {
                    now = now.AddDays(1);
                }
                return now;
            }

            return now;
        }

        public DateTime? GetDueDateFromStartForInterval(DateTime startDate)
        {
            switch (Interval)
            {
                case StrategyInterval.None:
                    return null;

                case StrategyInterval.Days:
                    return startDate.AddDays(1);

                case StrategyInterval.Weeks:
                    return startDate.AddDays(4);

                case StrategyInterval.Months:
                    return startDate.AddMonths(1);

                default:
                    return null;
            }
        }

        public DateTime GetNextStartDateForInterval(DateTime previousStartDate)
        {
            switch (this.Interval)
            {
                case StrategyInterval.Days: return previousStartDate.AddDays(1);
                case StrategyInterval.Weeks: return previousStartDate.AddDays(7);
                case StrategyInterval.Months: return previousStartDate.AddMonths(1);
                default:
                    return previousStartDate;
            }
        }

        public void MarkOrder()
        {
            if (Goals == null)
            {
                return;
            }

            int order = 0;
            foreach (StrategyGoal sg in Goals)
            {
                sg.Goal.Ordinal = order;
                ++order;
            }
        }

        #endregion

        #region Adopt Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <param name="companyId"></param>
        public void Adopt(ApplicationDbContext context, string userId, int companyId)
        {
            StrategyAdoption adoption = context.StrategyAdoptions.Create(userId, companyId, this);

            DateTime goalStartDate = GetFirstStartDateForIntervalFromNow();
            DateTime? goalDueDate = GetDueDateFromStartForInterval(goalStartDate);

            foreach (StrategyGoal strategyGoal in this.Goals)
            {
                Goal goal = strategyGoal.Goal;

                Goal newGoal = context.Goals.CreateFromTemplate(context.WorkItems, goal, companyId, userId);

                newGoal.StartDateUtc = goalStartDate;
                newGoal.DueDateUtc = goalDueDate;

                adoption.Goals.Add(newGoal);

                // Calculate next
                goalStartDate = GetNextStartDateForInterval(goalStartDate);
                goalDueDate = GetDueDateFromStartForInterval(goalStartDate);
            }
        }

        #endregion

        #region ICommentable

        [NotMapped]
        public string CommentControllerName { get { return "Strategies"; } }

        [NotMapped]
        public string CommentActionName { get { return "Details"; } }

        #endregion
    }


    public static class StrategyExtensions
    {
        public static IQueryable<Strategy> WhereMadeByUser(this DbSet<Strategy> strategies, string userId)
        {
            return strategies.Where(s => s.UserId == userId && !s.IsDeleted).Include(s => s.Goals).OrderByDescending(s => s.CreatedDateUtc);
        }

        public static IQueryable<Strategy> WhereLatestFive(this DbSet<Strategy> strategies)
        {
            return strategies.Where(s => !s.IsDeleted).Include(s => s.User).OrderByDescending(s => s.CreatedDateUtc).Take(5);
        }

        public static IQueryable<Strategy> SearchStrategies(this ApplicationDbContext context)
        {
            return context.Strategies.Where(s => s.IsDeleted == false).Include(s => s.User).Include(s => s.Adoptions).Include(s => s.Ratings);
        }
    }
}