using System;
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

        /// <summary>
        /// Called when a user verifies their e-mail address, confirming their Pando Logic account
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// The page for accepting or declining an invitation to a team on pando logic
        /// </summary>
        /// <returns></returns>
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
                Activity newActivity = Db.Activities.Create(currentUser.Id, "");
                string linkTitle = string.Format("{0} joined {1}", currentUser.FullName, invite.Company.Name);
                newActivity.SetTitle(linkTitle, Url.Action("Details", "Users", currentUser.Id));
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

        /// <summary>
        /// Called when the user chooses to decline the invite into a team, deleting it from the system
        /// </summary>
        /// <param name="inviteId"></param>
        /// <returns></returns>
        [HttpPost]
        [InviteOnly]
        public async Task<ActionResult> DeclineInvite(int inviteId)
        {
            MemberInvite invite = await Db.MemberInvites.FindAsync(inviteId);

            if (Request.IsAuthenticated)
            {
                // Save an activity for this user
                ApplicationUser currentUser = await GetCurrentUserAsync();
                Activity newActivity = Db.Activities.Create(currentUser.Id, "");
                string linkTitle = string.Format("{0} declined to join {1}", currentUser.FullName, invite.Company.Name);
                newActivity.SetTitle(linkTitle, Url.Action("Details", "Users", currentUser.Id));
                newActivity.Type = ActivityType.TeamNotification;
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