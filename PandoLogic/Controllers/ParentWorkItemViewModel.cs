using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PandoLogic.Models;
using System.ComponentModel.DataAnnotations;

using Masticore;

namespace PandoLogic.Controllers
{
    [NotMapped]
    public class ChildWorkItemViewModel
    {
        public ChildWorkItemViewModel() { }

        public ChildWorkItemViewModel(StrategyGoal strategyGoal)
        {
            this.Title = strategyGoal.Goal.Title;
            this.Description = strategyGoal.Goal.Description;
            this.Id = strategyGoal.GoalId;
            this.RelationshipId = strategyGoal.Id;
        }

        public ChildWorkItemViewModel(WorkItem task)
        {
            this.Title = task.Title;
            this.Description = task.Description;
            this.Id = task.Id;
        }

        public int? RelationshipId { get; set; }
        public int? Id { get; set; }
        public int Ordinal { get; set; }

        public bool IsMarkedForDelete { get; set; }

        [ConditionalRequired(IgnoreFlagName = "IsMarkedForDelete", IgnoreFlagValue = true)]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        [ConditionalRequired(IgnoreFlagName = "IsMarkedForDelete", IgnoreFlagValue = true)]
        public string Description { get; set; }
    }

    [NotMapped]
    public class ParentWorkItemViewModel
    {
        #region Properties

        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [ConditionalRequired(IgnoreFlagName = "IsSummaryRequired", IgnoreFlagValue = false)]
        [MaxLength(200)]
        public string Summary { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public StrategyInterval Interval { get; set; }

        public bool IsSummaryRequired { get; set; }

        public List<ChildWorkItemViewModel> Children { get; set; }

        #endregion

        #region Methods

        public ParentWorkItemViewModel() { }

        public ParentWorkItemViewModel(Strategy strategy)
        {
            Id = strategy.Id;
            Title = strategy.Title;
            Summary = strategy.Summary;
            Description = strategy.Description;

            Children = new List<ChildWorkItemViewModel>();

            // Load up the children if it already has some
            if (strategy.Goals != null && strategy.Goals.Count > 0)
            {
                foreach (StrategyGoal strategyGoal in strategy.Goals)
                {
                    Children.Add(new ChildWorkItemViewModel(strategyGoal));
                }
            }
        }

        public ParentWorkItemViewModel(Goal goal)
        {
            Id = goal.Id;
            Title = goal.Title;
            Description = goal.Description;
            Children = new List<ChildWorkItemViewModel>();

            // Load up the children if it already has some
            if (goal.WorkItems != null && goal.WorkItems.Count > 0)
            {
                foreach (WorkItem workItem in goal.WorkItems)
                {
                    Children.Add(new ChildWorkItemViewModel(workItem));
                }
            }
        }

        public void CreateChildren(int count = 1)
        {
            if (Children == null)
            {
                Children = new List<ChildWorkItemViewModel>();
            }
            for (int i = 0; i < count; i++)
            {
                Children.Add(new ChildWorkItemViewModel());
            }
        }

        public void MarkOrder()
        {
            int i = 1;
            foreach (ChildWorkItemViewModel phase in Children)
            {
                phase.Ordinal = i;
                ++i;
            }
        }

        #endregion
    }
}
