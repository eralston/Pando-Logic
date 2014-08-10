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
    public class UsersController : BaseController
    {
        //// GET: ApplicationUsers
        //public async Task<ActionResult> Index()
        //{
        //    var applicationUsers = Db.ApplicationUsers.Include(a => a.VerificationInvite);
        //    return View(await applicationUsers.ToListAsync());
        //}

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
                ViewBag.AssignedTasks = await Db.WorkItems.WhereAssignedUserAndCompany(UserCache.Id, member.CompanyId).Where(t => t.CompletedDate == null).ToArrayAsync();
            }

            return View(applicationUser);
        }

        //// GET: ApplicationUsers/Create
        //public ActionResult Create()
        //{
        //    ViewBag.VerificationInviteId = new SelectList(Db.Invites, "Id", "Value");
        //    return View();
        //}

        //// POST: ApplicationUsers/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Create([Bind(Include = "Id,FirstName,LastName,AvatarUrl,AvatarFileName,VerificationInviteId,Email,EmailConfirmed,PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName")] ApplicationUser applicationUser)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        Db.ApplicationUsers.Add(applicationUser);
        //        await Db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.VerificationInviteId = new SelectList(Db.Invites, "Id", "Value", applicationUser.VerificationInviteId);
        //    return View(applicationUser);
        //}

        //// GET: ApplicationUsers/Edit/5
        //public async Task<ActionResult> Edit(string id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    ApplicationUser applicationUser = await Db.ApplicationUsers.FindAsync(id);
        //    if (applicationUser == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.VerificationInviteId = new SelectList(Db.Invites, "Id", "Value", applicationUser.VerificationInviteId);
        //    return View(applicationUser);
        //}

        //// POST: ApplicationUsers/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit([Bind(Include = "Id,FirstName,LastName,AvatarUrl,AvatarFileName,VerificationInviteId,Email,EmailConfirmed,PasswordHash,SecurityStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEndDateUtc,LockoutEnabled,AccessFailedCount,UserName")] ApplicationUser applicationUser)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        Db.Entry(applicationUser).State = EntityState.Modified;
        //        await Db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.VerificationInviteId = new SelectList(Db.Invites, "Id", "Value", applicationUser.VerificationInviteId);
        //    return View(applicationUser);
        //}

        //// GET: ApplicationUsers/Delete/5
        //public async Task<ActionResult> Delete(string id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    ApplicationUser applicationUser = await Db.ApplicationUsers.FindAsync(id);
        //    if (applicationUser == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(applicationUser);
        //}

        //// POST: ApplicationUsers/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteConfirmed(string id)
        //{
        //    ApplicationUser applicationUser = await Db.ApplicationUsers.FindAsync(id);
        //    Db.ApplicationUsers.Remove(applicationUser);
        //    await Db.SaveChangesAsync();
        //    return RedirectToAction("Index");
        //}
    }
}
