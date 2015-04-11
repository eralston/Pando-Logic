using InviteOnly;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using StripeEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PandoLogic.Models
{
    /// <summary>
    /// View model for safe application user fields
    /// </summary>
    public class ApplicationUserViewModel
    {
        public ApplicationUserViewModel(ApplicationUser user)
        {
            this.Id = user.Id;
            this.AvatarUrl = user.AvatarOrDefaultUrl;
            this.FullName = user.FullName;
            this.FirstName = user.FirstName;
            this.LastName = user.LastName;
            this.UserUrl = user.UserUrl;
        }

        public string Id { get; set; }
        public string AvatarUrl { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserUrl { get; set; }
    }

    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser, IStripeUser
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

        public DateTime? CreatedDate { get; set; }

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

        /// <summary>
        /// Gets the URL for the current user
        /// </summary>
        public string UserUrl
        {
            get
            {
                return string.Format("/Users/Details/{0}", Id);
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