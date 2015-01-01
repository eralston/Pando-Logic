using System;
using System.Collections.Generic;
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
    /// Maps from an ApplicationUser to a Company model
    /// </summary>
    public class Member : BaseModel
    {
        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        /// <summary>
        /// A flag for which company is currently selected as the context for the UI
        /// The member with 
        /// NOTE: In practice, this is secondary to the user's session information
        /// </summary>
        public DateTime? LastSelectedDate { get; set; }

        /// <summary>
        /// Sets the last selected date for this member record
        /// NOTE: Be sure to save changes to the active DB context after calling this function
        /// </summary>
        public void SetSelected()
        {
            LastSelectedDate = DateTime.Now;
        }
    }

    public static class MemberExtesions
    {
        public static Member Create(this DbSet<Member> members, ApplicationUser user, Company company)
        {
            Member member = new Member();

            member.CreatedDate = DateTime.Now;
            member.Company = company;
            member.User = user;
            member.LastSelectedDate = member.CreatedDate;

            members.Add(member);

            return member;
        }

        /// <summary>
        /// Finds the most recently selected member record for the given user
        /// </summary>
        /// <param name="members"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static IQueryable<Member> FindSelectedForUser(this DbSet<Member> members, ApplicationUser user)
        {
            // Find all members for the current user
            var userMembers = members.WhererUserIsMember(user).OrderByDescending(m => m.LastSelectedDate).Include(m => m.Company);
            return userMembers;
        }

        public static IQueryable<Member> WhererUserIsMember(this DbSet<Member> members, ApplicationUser user)
        {
            string userId = user.Id;
            // Find all members for the current user
            var userMembers = members.Where(m => m.UserId == userId);
            return userMembers;
        }

        public static IQueryable<Member> WhereCompany(this DbSet<Member> members, Company company)
        {
            int companyId = company.Id;
            return members.Where(m => m.CompanyId == companyId).Include(m => m.User);
        }

        public static Task<Member> FindPrimaryForUser(this DbSet<Member> members, string userId)
        {
            return members.Where(m => m.UserId == userId).FirstOrDefaultAsync();
        }

        public static Task<Member> WhereAssignedToUserAndCompany(this DbSet<Member> members, string userId, int companyId)
        {
            return members.Where(m => m.UserId == userId && m.CompanyId == companyId).FirstOrDefaultAsync();
        }
    }
}