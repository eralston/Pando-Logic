﻿using System;
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
    /// An invitation for a user not yet in the system that connects a company to just an e-mail address
    /// Onboarding a new team member converts this record into a new Member object onces an ApplicationUser exists
    /// </summary>
    public class MemberInvite
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        [Display(Name = "Fulfilled Date")]
        public DateTime? FulfilledDate { get; set; }

        // To-One on Company
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on Invite
        [ForeignKey("Invite")]
        public int InviteId { get; set; }
        public virtual InviteOnly.Invite Invite { get; set; }

        [Display(Name = "E-Mail")]
        [EmailAddress]
        public string Email { get; set; }
    }

    public static class MemberInviteExtesions
    {
        /// <summary>
        /// Creates a new instance of a member invite using the given e-mail, including creating the related invite record
        /// </summary>
        /// <param name="memberInvites"></param>
        /// <param name="email"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static MemberInvite Create(this DbSet<MemberInvite> memberInvites, string email, int companyId)
        {
            MemberInvite invite = new MemberInvite();

            invite.CreatedDate = DateTime.Now;
            invite.CompanyId = companyId;
            invite.Email = email;

            // Create a new invite for this
            invite.Invite = InviteOnly.Invite.Create();

            memberInvites.Add(invite);

            return invite;
        }

        /// <summary>
        /// Finds the most recently selected member record for the given user
        /// </summary>
        /// <param name="members"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static IQueryable<MemberInvite> WhereCompany(this DbSet<MemberInvite> memberInvites, int companyId)
        {
            // Find all members for the current user
            var invites = memberInvites.Where(u => u.CompanyId == companyId).OrderBy(u => u.Email);
            return invites;
        }

        /// <summary>
        /// Finds the memberinvite connected to the given invite OR null if not found
        /// </summary>
        /// <param name="memberInvites"></param>
        /// <param name="invite"></param>
        /// <returns></returns>
        public static Task<MemberInvite> FindForInvite(this DbSet<MemberInvite> memberInvites, InviteOnly.Invite invite)
        {
            int inviteId = invite.Id;
            return memberInvites.Where(i => i.InviteId == inviteId).FirstOrDefaultAsync();
        }
    }
}