using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PandoLogic.Models
{
    /// <summary>
    /// A subscription plan for Pando Logic that holds flags for enabling and disabling features
    /// </summary>
    public class Plan : StripeEntities.SubscriptionPlan
    {
        [Editable(true)]
        [Display(Name = "Consulting Discount")]
        public int ConsultingDiscount { get; set; }

        [Editable(true)]
        [Display(Name = "Influencer Limit")]
        public int InfluencerLimit { get; set; }

        [Editable(true)]
        [Display(Name = "Team Member Limit")]
        public int TeamMemberLimit { get; set; }

        [Editable(true)]
        [Display(Name = "Monthly Strategies")]
        public bool IsMonthlyStrategiesEnabled { get; set; }

        [Editable(true)]
        [Display(Name = "Strategy Exchange")]
        public bool IsStrategyExchangeEnabled { get; set; }

        [Editable(true)]
        [AllowHtml]
        public string OfferHtml { get; set; }
    }

    /// <summary>
    /// Static class for extension methods associated with plans
    /// </summary>
    public static class PlanExtensions
    {
        public static IQueryable<Plan> AllAvailablePlans(this DbSet<Plan> plans)
        {
            return plans.Where(p => p.State == StripeEntities.SubscriptionPlan.SubscriptionState.Available);
        }
    }
}