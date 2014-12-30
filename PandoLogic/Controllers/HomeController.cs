﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using InviteOnly;

using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// Controller for actions at the root level of the app
    /// </summary>
    public class HomeController : BaseController
    {
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [InviteOnly]
        public async Task<ActionResult> Verify()
        {
            Invite invite = this.GetCurrentInvite();

            if (invite != null)
            {
                // Remove the invite
                ApplicationUser user = await GetCurrentUserAsync();
                user.VerificationInvite = null;
                Db.Invites.Remove(invite);
                await Db.SaveChangesAsync();
                return View();
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        [InviteOnly]
        public async Task<ActionResult> AcceptInvite()
        {
            Invite invite = this.GetCurrentInvite();
            MemberInvite memberInvite = await Db.MemberInvites.FindForInviteAsync(invite);

            if (invite != null)
            {
                return View(memberInvite);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Called when the user proceeds with accepting the invitation to the system
        /// This has different handling for registerd and unregistered users
        /// </summary>
        /// <param name="inviteId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> AcceptInvite(int inviteId)
        {
            MemberInvite invite = await Db.MemberInvites.FindAsync(inviteId);

            if (Request.IsAuthenticated)
            {
                // Save an activity for this user
                ApplicationUser currentUser = await GetCurrentUserAsync();
                Activity newActivity = Db.Activities.Create(currentUser.Id, "Accepted Invitation");
                string description = string.Format("You accepted the invite to join the {0} team", invite.Company.Name);
                newActivity.Description = description;
                newActivity.Type = ActivityType.TeamNotification;

                // Add them to the company (and refresh user info)
                Member membership = Db.Members.Create(currentUser, invite.Company);
                ClearCurrentUserCache();

                // clear the invite
                invite.FulfilledDate = DateTime.Now;
                Db.Invites.Remove(invite.Invite);
                invite.Invite = null;

                // Save changes
                await Db.SaveChangesAsync();

                // Send them home
                return RedirectToAction("Index");
            }
            else
            {
                // Tell them to register
                return RedirectToAction("Register", "Account");
            }
        }

        [HttpPost]
        [InviteOnly]
        public async Task<ActionResult> DeclineInvite(int inviteId)
        {
            MemberInvite invite = await Db.MemberInvites.FindAsync(inviteId);

            if (Request.IsAuthenticated)
            {
                // Save an activity for this user
                ApplicationUser currentUser = await GetCurrentUserAsync();
                Activity newActivity = Db.Activities.Create(currentUser.Id, "Declined Invitation");
                string description = string.Format("You declined the invite to join the {0} team", invite.Company.Name);
                newActivity.Description = description;
                newActivity.Type = ActivityType.WorkDeleted;
            }

            // clear the invite
            invite.FulfilledDate = DateTime.Now;
            Db.Invites.Remove(invite.Invite);
            invite.Invite = null;

            // Save changes
            await Db.SaveChangesAsync();

            // Send them home
            return RedirectToAction("Index");
        }
    }
}