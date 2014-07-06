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
    /// Maps from an ApplicationUser to a Company model
    /// </summary>
    public class Member
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

    public static class MemberExtesions
    {
        public static Member Create(this DbSet<Member> members, ApplicationUser user, Company company)
        {
            Member member = new Member();

            member.CreatedDate = DateTime.Now;
            member.Company = company;
            member.User = user;

            members.Add(member);

            return member;
        }
    }
}