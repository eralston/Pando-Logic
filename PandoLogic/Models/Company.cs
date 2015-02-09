﻿using System;
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
    /// <summary>
    /// An organization using Pando Logic to track its progress
    /// </summary>
    public class Company : BaseModel
    {
        // To-One on ApplicationUser
        [ForeignKey("Creator")]
        public string CreatorId { get; set; }
        public virtual ApplicationUser Creator { get; set; }

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

        [Display(Name = "Founded Date")]
        [DataType(DataType.DateTime)]
        public DateTime? FoundedDate { get; set; }

        [DefaultValue(false)]
        public bool IsSoftDeleted { get; set; }
    }

    public static class CompanyExtensions
    {
        public static Company Create(this DbSet<Company> companies, ApplicationUser creatorUser)
        {
            Company company = new Company();

            company.CreatedDate = DateTime.UtcNow;
            company.Creator = creatorUser;

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