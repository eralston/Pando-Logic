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
    public class StrategyAdoption
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }

        public virtual ICollection<Goal> Goals { get; set; }
    }

    public static class StrategyAdoptionExtensions
    {
        public static StrategyAdoption Create(this DbSet<StrategyAdoption> adoptions, string userId, int companyId, Strategy strategy)
        {
            StrategyAdoption adoption = adoptions.Create();

            adoption.CreatedDate = DateTime.Now;

            adoption.Strategy = strategy;
            adoption.UserId = userId;
            adoption.CompanyId = companyId;
            adoption.Goals = new List<Goal>();

            adoptions.Add(adoption);

            return adoption;
        }
    }
}