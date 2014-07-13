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
    public class ActivitiesController : BaseController
    {
        // GET: Activities
        public async Task<ActionResult> Index()
        {
            var activities = Db.Activities.Include(a => a.Author).Include(a => a.Company);
            return View(await activities.ToListAsync());
        }

        // GET: Activities/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity activity = await Db.Activities.FindAsync(id);
            if (activity == null)
            {
                return HttpNotFound();
            }
            return View(activity);
        }

        // GET: Activities/Create
        public ActionResult Create()
        {
            ViewBag.AuthorId = new SelectList(Db.Users, "Id", "FirstName");
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId");
            return View();
        }

        // POST: Activities/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,CreatedDate,Title,Description,ActivityType,AuthorId,CompanyId")] Activity activity)
        {
            if (ModelState.IsValid)
            {
                Db.Activities.Add(activity);
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.AuthorId = new SelectList(Db.Users, "Id", "FirstName", activity.AuthorId);
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", activity.CompanyId);
            return View(activity);
        }

        // GET: Activities/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity activity = await Db.Activities.FindAsync(id);
            if (activity == null)
            {
                return HttpNotFound();
            }
            ViewBag.AuthorId = new SelectList(Db.Users, "Id", "FirstName", activity.AuthorId);
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", activity.CompanyId);
            return View(activity);
        }

        // POST: Activities/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,CreatedDate,Title,Description,ActivityType,AuthorId,CompanyId")] Activity activity)
        {
            if (ModelState.IsValid)
            {
                Db.Entry(activity).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.AuthorId = new SelectList(Db.Users, "Id", "FirstName", activity.AuthorId);
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", activity.CompanyId);
            return View(activity);
        }

        // GET: Activities/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity activity = await Db.Activities.FindAsync(id);
            if (activity == null)
            {
                return HttpNotFound();
            }
            return View(activity);
        }

        // POST: Activities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Activity activity = await Db.Activities.FindAsync(id);
            Db.Activities.Remove(activity);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
