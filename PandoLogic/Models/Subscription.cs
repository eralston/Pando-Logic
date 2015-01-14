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
    /// A model for connection users to their subscriptions
    /// There should be one instance for each payment relationship between PandoLogic and a customer (one for each company)
    /// </summary>
    public class Subscription : BaseModel
    {
        [Display(Name = "Active Until")]
        public DateTime? ActiveUntil { get; set; }

        public string Notes { get; set; }

        public string PaymentSystemId { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // To-One on SubscriptionPlan
        [ForeignKey("Plan")]
        public int PlanId { get; set; }
        public virtual SubscriptionPlan Plan { get; set; }
    }

    public static class SubscriptionExtensions
    {
        /// <summary>
        /// Creates a new subscription
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="user"></param>
        /// <param name="company"></param>
        /// <param name="plan"></param>
        /// <returns></returns>
        public static Subscription Create(this DbSet<Subscription> subscriptions, ApplicationUser user, Company company, SubscriptionPlan plan)
        {
            Subscription newSub = new Subscription();

            newSub.CreatedDate = DateTime.Now;

            newSub.Company = company;
            newSub.User = user;
            newSub.Plan = plan;

            subscriptions.Add(newSub);

            return newSub;
        }
    }
}