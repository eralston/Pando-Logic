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
        public int SessionInviteId
        {
            get
            {
                object o = Session["MemberInviteId"];
                if (o == null)
                    return -1;
                return (int)o;
            }
            set
            {
                Session["MemberInviteId"] = value;
            }
        }

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [InviteOnly]
        public async Task<ActionResult> Verify()
        {
            Invite invite = this.GetCurrentInvite();

            if(invite != null)
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
                // Save the invite ID to the session
                SessionInviteId = invite.Id;

                if(Request.IsAuthenticated)
                {
                    return View(memberInvite);
                }
                else
                {
                    return View(memberInvite);
                }
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
            
            if(Request.IsAuthenticated)
            {
                // Fulfill the invite
                invite.FulfilledDate = DateTime.Now;

                // clear the invite
                Db.Invites.Remove(invite.Invite);
                invite.Invite = null;

                // Save changes
                await Db.SaveChangesAsync();

                // Send them home
                return RedirectToAction("Index");
            }
            else
            {
                // Save this invite for later and send them in for registration
                
                return RedirectToAction("Register", "Account");
            }            
        }
    }
}