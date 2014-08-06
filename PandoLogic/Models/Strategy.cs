using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Security;

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
        [ForeignKey("Author")]
        public string AuthorId { get; set; }
        public virtual ApplicationUser Author { get; set; }

        [Display(Name = "Goal Interval")]
        public StrategyInterval Interval { get; set; }

        // To-Many on StrategyGoal
        public virtual ICollection<StrategyGoal> Goals { get; set; }


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
            
            // Set the fixed fields
            newStrategyGoal.CreatedDate = DateTime.Now;
            newGoalTemplate.CreatedDate = newStrategyGoal.CreatedDate;
            newGoalTemplate.IsTemplate = true;

            // Map the existing goal into the new goal
            newGoalTemplate.Title = existingGoal.Title;
            newGoalTemplate.Description = existingGoal.Description;

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
            DateTime now = DateTime.Now;

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
                while(now.Day != 1)
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

        public DateTime GetNextStartDateForInterval(DateTime previousStartDate, StrategyInterval interval)
        {
            switch(interval)
            {
                case StrategyInterval.Days: return previousStartDate.AddDays(1);
                case StrategyInterval.Weeks: return previousStartDate.AddDays(7);
                case StrategyInterval.Months: return previousStartDate.AddMonths(1);
                default:
                    return previousStartDate;
            }
        }

        #endregion
    }
}