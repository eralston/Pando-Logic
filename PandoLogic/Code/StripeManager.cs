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
            newStripePlan.Id = subscriptionPlan.PaymentSystemId;

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
            planService.Update(plan.PaymentSystemId, options);
        }

        /// <summary>
        /// Deletes a plan from Stripe
        /// NOTE: Delete the model from the underlying context after calling this method
        /// </summary>
        /// <param name="subscriptionPlan"></param>
        public static void DeletePlan(SubscriptionPlan subscriptionPlan)
        {
            var planService = new StripePlanService();
            planService.Delete(subscriptionPlan.PaymentSystemId);
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
            var subscriptionService = new StripeSubscriptionService();
            StripeSubscription stripeSubscription = subscriptionService.Create(subscription.User.PaymentSystemId, subscription.Plan.PaymentSystemId);
            subscription.PaymentSystemId = stripeSubscription.Id;
        }

        /// <summary>
        /// Unsubscribes the given subscription
        /// NOTE: Save changes on the underlying context for the model after calling this method
        /// </summary>
        /// <param name="subscription"></param>
        public static void Unsubscribe(Subscription subscription)
        {
            var subscriptionService = new StripeSubscriptionService();
            subscriptionService.Cancel(subscription.PaymentSystemId, subscription.User.PaymentSystemId);
            subscription.PaymentSystemId = null;
        }
    }
}