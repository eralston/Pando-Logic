using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// Controller for user information in the system
    /// </summary>
    public class UsersController : BaseController
    {
        // GET: ApplicationUsers/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser applicationUser = (await Db.Users.Where(u => u.Id == id).FirstOrDefaultAsync()) as ApplicationUser;
            if (applicationUser == null)
            {
                return HttpNotFound();
            }

            ViewBag.UserStrategies = await Db.Strategies.WhereMadeByUser(id).ToArrayAsync();
            Member member = await Db.Members.FindPrimaryForUser(id);
            ViewBag.UserMember = member;

            if (ViewBag.UserMember != null)
            {
                ViewBag.AssignedTasks = await Db.WorkItems.WhereAssignedUserAndCompany(UserCache.Id, member.CompanyId).Where(t => t.CompletedDateUtc == null).ToArrayAsync();
            }

            return View(applicationUser);
        }

        /// <summary>
        /// Returns the given user's basic details as json
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> DetailsJson(string id)
        {
            ApplicationUser applicationUser = (await Db.Users.Where(u => u.Id == id).FirstOrDefaultAsync()) as ApplicationUser;
            ApplicationUserViewModel userModel = new ApplicationUserViewModel(applicationUser);
            return Json(userModel, JsonRequestBehavior.AllowGet);
        }
    }
}
