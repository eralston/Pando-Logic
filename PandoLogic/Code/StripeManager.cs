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
    /// 
    /// 1) Create a plan
    /// 2) Create a user (with payment token)
    /// 3) Subscribe a user to a plan
    /// </summary>
    public class StripeManager
    {
        /// <summary>
        /// Creates a new plan inside of Stripe, using the given subscription plan's information
        /// </summary>
        /// <param name="plan"></param>
        public static void CreatePlan(SubscriptionPlan plan)
        {
            // Save it to Stripe
            StripePlanCreateOptions newStripePlanOptions = new StripePlanCreateOptions();
            newStripePlanOptions.Amount = Convert.ToInt32(plan.Price * 100.0); // all amounts on Stripe are in cents, pence, etc
            newStripePlanOptions.Currency = "usd";                                 // "usd" only supported right now
            newStripePlanOptions.Interval = "month";                               // "month" or "year"
            newStripePlanOptions.IntervalCount = 1;                                // optional
            newStripePlanOptions.Name = plan.Title;
            newStripePlanOptions.TrialPeriodDays = plan.TrialDays;     // amount of time that will lapse before the customer is billed
            newStripePlanOptions.Id = plan.PaymentSystemId;

            StripePlanService planService = new StripePlanService();
            StripePlan newPlan = planService.Create(newStripePlanOptions);
            plan.PaymentSystemId = newPlan.Id;

            System.Diagnostics.Trace.TraceInformation("Created new in stripe: '{0}' with id {1}", plan.Title, plan.PaymentSystemId);
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
            planService.Update(plan.PaymentSystemId, options);

            System.Diagnostics.Trace.TraceInformation("Updated plan in stripe: '{0}' with id '{1}'", plan.Title, plan.PaymentSystemId);
        }

        /// <summary>
        /// Deletes a plan from Stripe
        /// NOTE: Delete the model from the underlying context after calling this method
        /// </summary>
        /// <param name="plan"></param>
        public static void DeletePlan(SubscriptionPlan plan)
        {
            var planService = new StripePlanService();
            planService.Delete(plan.PaymentSystemId);

            System.Diagnostics.Trace.TraceInformation("Deleting plan in stripe: '{0}' with id '{1}", plan.Title, plan.PaymentSystemId);

            plan.PaymentSystemId = null;
        }

        /// <summary>
        /// Creates a new customer record in Stripe for the given user
        /// NOTE: Save changes on the underlying context for the model after calling this method
        /// </summary>
        /// <param name="user"></param>
        public static void CreateCustomer(ApplicationUser user, string paymentToken = null)
        {
            // Do not overwrite the user, ever
            if (user.HasPaymentInfo)
                return;

            var newCustomer = new StripeCustomerCreateOptions();

            newCustomer.Email = user.Email;

            if (paymentToken != null)
                newCustomer.TokenId = paymentToken;

            var customerService = new StripeCustomerService();
            StripeCustomer stripeCustomer = customerService.Create(newCustomer);

            // Set the accounting info
            user.PaymentSystemId = stripeCustomer.Id;

            System.Diagnostics.Trace.TraceInformation("Created customer in stripe: '{0}' with id '{1}", user.Email, user.PaymentSystemId);
        }

        /// <summary>
        /// Updates a customer record, using the given payment token
        /// NOTE: Save changes on the underlying context for the model after calling this method
        /// </summary>
        /// <param name="user"></param>
        /// <param name="paymentToken"></param>
        public static void UpdateCustomer(ApplicationUser user, string paymentToken = null)
        {
            var customerUpdate = new StripeCustomerUpdateOptions();

            // set these properties if it makes you happy
            customerUpdate.Email = user.Email;
            customerUpdate.TokenId = paymentToken;

            var customerService = new StripeCustomerService();
            StripeCustomer stripeCustomer = customerService.Update(user.PaymentSystemId, customerUpdate);

            System.Diagnostics.Trace.TraceInformation("Updated customer in stripe: '{0}' with id '{1}", user.Email, user.PaymentSystemId);
        }

        /// <summary>
        /// Creates or update a customer
        /// </summary>
        /// <param name="user"></param>
        /// <param name="paymentToken"></param>
        public static void CreateOrUpdateCustomer(ApplicationUser user, string paymentToken = null)
        {
            if (user.HasPaymentInfo)
                UpdateCustomer(user, paymentToken);
            else
                CreateCustomer(user, paymentToken);
        }

        /// <summary>
        /// Subscribes the given user to the given plan, using the payment information already in stripe for that user
        /// NOTE: Save changes on the underlying context for the model after calling this method
        /// </summary>
        /// <param name="subscription"></param>
        public static void Subscribe(Subscription subscription)
        {
            if (!string.IsNullOrEmpty(subscription.PaymentSystemId))
                return;

            var subscriptionService = new StripeSubscriptionService();
            StripeSubscription stripeSubscription = subscriptionService.Create(subscription.User.PaymentSystemId, subscription.Plan.PaymentSystemId);
            subscription.PaymentSystemId = stripeSubscription.Id;

            System.Diagnostics.Trace.TraceInformation("Subscribed customer in stripe: '{0}' with new subscription id '{1}", subscription.User.Email, subscription.PaymentSystemId);
        }

        /// <summary>
        /// Changes the given subscription to use the new plan
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="newPlan"></param>
        public static void ChangeSubscriptionPlan(Subscription subscription, SubscriptionPlan newPlan)
        {
            StripeSubscriptionUpdateOptions options = new StripeSubscriptionUpdateOptions();
            options.PlanId = newPlan.PaymentSystemId;

            subscription.Plan = newPlan;

            var subscriptionService = new StripeSubscriptionService();
            subscriptionService.Update(subscription.User.PaymentSystemId, subscription.PaymentSystemId, options);
        }

        /// <summary>
        /// Unsubscribes the given subscription
        /// NOTE: Save changes on the underlying context for the model after calling this method
        /// </summary>
        /// <param name="subscription"></param>
        public static void Unsubscribe(Subscription subscription)
        {
            if (string.IsNullOrEmpty(subscription.PaymentSystemId) || string.IsNullOrEmpty(subscription.User.PaymentSystemId))
                return;

            var subscriptionService = new StripeSubscriptionService();
            subscriptionService.Cancel(subscription.PaymentSystemId, subscription.User.PaymentSystemId);
            subscription.PaymentSystemId = null;

            System.Diagnostics.Trace.TraceInformation("Unsuscribed customer in stripe: '{0}' with new subscription id '{1}", subscription.User.Email, subscription.PaymentSystemId);
        }
    }
}