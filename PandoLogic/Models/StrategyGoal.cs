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
    public class StrategyGoal
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [ForeignKey("Goal")]
        public int GoalId { get; set; }
        public virtual Goal Goal { get; set; }

        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }
    }
}