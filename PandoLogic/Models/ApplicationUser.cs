﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

using InviteOnly;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PandoLogic.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        [NotMapped]
        [Display(Name = "Full Name")]
        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
        }

        [ForeignKey("Avatar")]
        public int? AvatarId { get; set; }
        public virtual CloudFile Avatar { get; set; }

        [NotMapped]
        public string AvatarOrDefaultUrl
        {
            get
            {
                if(Avatar != null)
                {
                    return Avatar.Url;
                }
                else
                {
                    return "/Content/images/user-icon.png";
                }
            }
        }

        // To-Many on PhoneNumber
        public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; }

        // To-One on Invite
        [ForeignKey("VerificationInvite")]
        public int? VerificationInviteId { get; set; }
        public virtual Invite VerificationInvite { get; set; }

        /// <summary>
        /// Customer ID for the charging system
        /// </summary>
        public string PaymentSystemId { get; set; }

        /// <summary>
        /// Returns true if this application user has payment information; otherwise, returns false
        /// </summary>
        public bool HasPaymentInfo 
        { 
            get
            {
                return !string.IsNullOrEmpty(this.PaymentSystemId);
            }
        }

        #region Methods

        [NotMapped]
        public bool IsVerified
        {
            get { return VerificationInvite == null; }
        }

        #endregion
    }

    public static class ApplicationUserExtensions
    {
        /// <summary>
        /// Gets a queryable for all users in the given company
        /// </summary>
        /// <param name="context"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        public static IQueryable<ApplicationUser> UsersInCompany(this ApplicationDbContext context, int companyId)
        {
            return from u in context.Users
                   join m in context.Members on u.Id equals m.UserId
                   join c in context.Companies on m.CompanyId equals c.Id
                   where c.Id == companyId
                   select u;
        }

        /// <summary>
        /// Finds the ApplicationUser associated with the given e-mail address, returning null if not found
        /// </summary>
        /// <param name="users"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public static ApplicationUser FindByEmail(this IQueryable<ApplicationUser> users, string email)
        {
            //Contract.Requires<ArgumentNullException>(email != null);

            return users.Where(u => u.Email == email).FirstOrDefault();
        }
    }
}