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
    [Authorize]
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

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
    }
}