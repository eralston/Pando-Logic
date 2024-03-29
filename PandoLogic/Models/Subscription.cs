﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;

namespace PandoLogic.Models
{
    /// <summary>
    /// A model for connection users to their subscriptions
    /// There should be one instance for each payment relationship between PandoLogic and a customer (one for each company)
    /// </summary>
    public class Subscription : StripeEntities.SubscriptionBase , IRequiredCompanyOwnedModel, IUserOwnedModel
    {
        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
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
        public static Subscription Create(this DbSet<Subscription> subscriptions, ApplicationUser user, Company company, StripeEntities.SubscriptionPlan plan)
        {
            Subscription newSub = new Subscription();

            newSub.CreatedDateUtc = DateTime.UtcNow;

            newSub.Company = company;
            newSub.User = user;
            newSub.Plan = plan;

            subscriptions.Add(newSub);

            return newSub;
        }

        /// <summary>
        /// Returns the single subscription for a given company
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static Task<Subscription> WhereCompany(this DbSet<Subscription> subscriptions, int companyId)
        {
            return subscriptions.Where(s => s.CompanyId == companyId && s.IsSoftDeleted == false).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns the single subscription linking between a user and a company
        /// NOTE: Fundamentally, there should be only one subscription per company; however, this validate the given user is attached to the subscription
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="userId"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static Task<Subscription> WhereUserAndCompany(this DbSet<Subscription> subscriptions, string userId, int companyId)
        {
            return subscriptions.Where(s => s.UserId == userId && s.CompanyId == companyId && s.IsSoftDeleted == false).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns a queryable for subscriptions for the given user
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static IQueryable<Subscription> WhereUser(this DbSet<Subscription> subscriptions, string userId)
        {
            return subscriptions.Where(s => s.UserId == userId && s.IsSoftDeleted == false);
        }
    }
}