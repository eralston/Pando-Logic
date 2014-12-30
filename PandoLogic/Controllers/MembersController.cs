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
    /// Controller for manager user membership in a company
    /// </summary>
    [Authorize]
    public class MembersController : BaseController
    {

        // GET: Members
        //public async Task<ActionResult> Index()
        //{
        //    var members = Db.Members.Include(m => m.Company).Include(m => m.User);
        //    return View(await members.ToListAsync());
        //}

        // GET: Members/Details/5
        //public async Task<ActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Member member = await Db.Members.FindAsync(id);
        //    if (member == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(member);
        //}

        // GET: Members/Create
        //public ActionResult Create()
        //{
        //    ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId");
        //    ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName");
        //    return View();
        //}

        // POST: Members/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Create([Bind(Include = "Id,CreatedDate,CompanyId,UserId,JobTitle,LastSelectedDate")] Member member)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        Db.Members.Add(member);
        //        await Db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", member.CompanyId);
        //    ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", member.UserId);
        //    return View(member);
        //}

        // GET: Members/Edit/5
        //public async Task<ActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Member member = await Db.Members.FindAsync(id);
        //    if (member == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", member.CompanyId);
        //    ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", member.UserId);
        //    return View(member);
        //}

        // POST: Members/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit([Bind(Include = "Id,CreatedDate,CompanyId,UserId,JobTitle,LastSelectedDate")] Member member)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        Db.Entry(member).State = EntityState.Modified;
        //        await Db.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", member.CompanyId);
        //    ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", member.UserId);
        //    return View(member);
        //}

        // GET: Members/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            Member member = await Db.Members.FindAsync(id);

            if (member == null)
            {
                return HttpNotFound();
            }

            // You can only manipulate members associated with you
            ApplicationUser user = await GetCurrentUserAsync();
            Member currentMember = await GetCurrentMemberAsync();

            if (member.UserId != user.Id && member.CompanyId != currentMember.CompanyId)
            {
                return HttpNotFound();
            }

            return View(member);
        }

        // POST: Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Member member = await Db.Members.FindAsync(id);

            if (member == null)
            {
                return HttpNotFound();
            }

            // You can only manipulate members associated with you
            ApplicationUser user = await GetCurrentUserAsync();
            if (member.UserId != user.Id)
            {
                return HttpNotFound();
            }

            // Remove from the database
            Db.Members.Remove(member);
            await Db.SaveChangesAsync();

            // Refresh the current user's cache
            ClearCurrentUserCache();

            return RedirectToAction("Index", "Home");
        }
    }
}
