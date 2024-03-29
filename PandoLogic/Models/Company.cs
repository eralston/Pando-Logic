﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Security;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// An organization using BizSprout to track its progress
    /// </summary>
    public class Company : Masticore.ModelBase, IUserOwnedModel
    {
        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Required Field
        [Display(Name = "Company Name")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Number of Employees")]
        public int NumberOfEmployees { get; set; }

        // To-One on Industry
        [ForeignKey("Industry")]
        public int IndustryId { get; set; }
        public virtual Industry Industry { get; set; }

        // To-Many on PhoneNumber
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; }

        // To-Many on PhoneNumber
        public virtual ICollection<Member> Members { get; set; }

        // To-One on Address
        [ForeignKey("Address")]
        public int? AddressId { get; set; }
        public virtual Address Address { get; set; }

        [ForeignKey("Avatar")]
        public int? AvatarId { get; set; }
        public virtual CloudFile Avatar { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string ZipCode { get; set; }

        /// <summary>
        /// Gets or sets the founding date for this entity
        /// NOTE: This should be translated from the user's local time then stored in UTC
        /// </summary>
        [Display(Name = "Founded Date")]
        [DataType(DataType.DateTime)]
        public DateTime? FoundedDateUtc { get; set; }
    }

    public static class CompanyExtensions
    {
        public static Company Create(this DbSet<Company> companies, ApplicationUser creatorUser)
        {
            Company company = new Company();

            company.CreatedDateUtc = DateTime.UtcNow;
            company.User = creatorUser;

            companies.Add(company);

            return company;
        }

        /// <summary>
        /// Creates a query for companies where the given user is a member
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static IQueryable<Company> CompaniesWhereUserIsMember(this ApplicationDbContext context, string userId)
        {
            var query = from m in context.Members
                        join c in context.Companies on m.CompanyId equals c.Id
                        where m.UserId == userId && c.IsSoftDeleted == false
                        select c;
            return query;
        }
    }
}