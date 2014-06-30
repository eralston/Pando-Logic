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
    public class IndustriesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Industries
        public async Task<ActionResult> Index()
        {
            return View(await db.Industries.ToListAsync());
        }

        // GET: Industries/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Industry industry = await db.Industries.FindAsync(id);
            if (industry == null)
            {
                return HttpNotFound();
            }
            return View(industry);
        }

        // GET: Industries/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Industries/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Title")] Industry industry)
        {
            if (ModelState.IsValid)
            {
                industry.CreatedDate = DateTime.Now;
                db.Industries.Add(industry);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(industry);
        }

        // GET: Industries/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Industry industry = await db.Industries.FindAsync(id);
            if (industry == null)
            {
                return HttpNotFound();
            }
            return View(industry);
        }

        // POST: Industries/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title")] Industry industry)
        {
            if (ModelState.IsValid)
            {
                Industry origIndustry = await db.Industries.FindAsync(industry.Id);
                origIndustry.Title = industry.Title;
                db.Entry(origIndustry).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(industry);
        }

        // GET: Industries/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Industry industry = await db.Industries.FindAsync(id);
            if (industry == null)
            {
                return HttpNotFound();
            }
            return View(industry);
        }

        // POST: Industries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Industry industry = await db.Industries.FindAsync(id);
            db.Industries.Remove(industry);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
