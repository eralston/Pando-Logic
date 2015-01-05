using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Stripe;

using PandoLogic.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace PandoLogic
{
    /// <summary>
    /// Wraps all of the functionality for stripe into a helper class taking only models as input and out
    /// Uses Stripe.Net https://github.com/jaymedavis/stripe.net
    /// This should make converting this functionality into another technology easier in the future
    /// </summary>
    public class StripeManager
    {
        /// <summary>
        /// Creates a new plan inside of Stripe, using the given subscription plan's information
        /// </summary>
        /// <param name="subscriptionPlan"></param>
        public static void CreatePlan(SubscriptionPlan subscriptionPlan)
        {
            // Save it to Stripe
            StripePlanCreateOptions newStripePlan = new StripePlanCreateOptions();
            newStripePlan.Amount = Convert.ToInt32(subscriptionPlan.Price * 100.0); // all amounts on Stripe are in cents, pence, etc
            newStripePlan.Currency = "usd";                                 // "usd" only supported right now
            newStripePlan.Interval = "month";                               // "month" or "year"
            newStripePlan.IntervalCount = 1;                                // optional
            newStripePlan.Name = subscriptionPlan.Title;
            newStripePlan.TrialPeriodDays = subscriptionPlan.TrialDays;     // amount of time that will lapse before the customer is billed
            newStripePlan.Id = subscriptionPlan.Identifier;

            StripePlanService planService = new StripePlanService();
            planService.Create(newStripePlan);
        }

        /// <summary>
        /// Updates the given plan
        /// NOTE: Due to limitatons with Stripe, this can only update the name of the plan
        /// </summary>
        /// <param name="plan"></param>
        public static void UpdatePlan(SubscriptionPlan plan)
        {
            StripePlanUpdateOptions options = new StripePlanUpdateOptions();
            options.Name = plan.Title;

            StripePlanService planService = new StripePlanService();
            planService.Update(plan.Identifier, options);
        }

        /// <summary>
        /// Deletes a plan from Stripe
        /// </summary>
        /// <param name="subscriptionPlan"></param>
        public static void DeletePlan(SubscriptionPlan subscriptionPlan)
        {
            var planService = new StripePlanService();
            planService.Delete(subscriptionPlan.Identifier);
        }

        /// <summary>
        /// Creates a new customer record in Stripe for the given user
        /// </summary>
        /// <param name="user"></param>
        public static void CreateCustomer(IdentityUser user)
        {
            var newCustomer = new StripeCustomerCreateOptions();

            // set these properties if it makes you happy
            newCustomer.Email = user.Email;

            var customerService = new StripeCustomerService();
            StripeCustomer stripeCustomer = customerService.Create(newCustomer);
        }
    }
}