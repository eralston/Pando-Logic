using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// A model for linking goals and strategies together
    /// </summary>
    public class StrategyGoal : BaseModel
    {
        [ForeignKey("Goal")]
        public int GoalId { get; set; }
        public virtual Goal Goal { get; set; }

        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }
    }
}