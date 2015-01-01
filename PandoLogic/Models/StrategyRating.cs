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
    public class StrategyRating : BaseModel
    {
        [Range(0.0f, 5.0f)]
        public float Rating { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }
    }

    public static class StrategyRatingExtensions
    {
        public static StrategyRating Create(this DbSet<StrategyRating> ratings, string userId, int strategyId)
        {
            StrategyRating rating = ratings.Create();

            rating.CreatedDate = DateTime.Now;
            rating.UserId = userId;
            rating.StrategyId = strategyId;

            ratings.Add(rating);

            return rating;
        }

        public static async Task<StrategyRating> FindForUserAsync(this DbSet<StrategyRating> ratings, string userId, int strategyId)
        {
            StrategyRating rating = await ratings.Where(sr => sr.StrategyId == strategyId && sr.UserId == userId).FirstOrDefaultAsync();
            return rating;
        }
    }
}