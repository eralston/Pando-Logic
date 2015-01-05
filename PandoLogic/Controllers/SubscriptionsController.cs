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
    [AdminAuthorize]
    public class SubscriptionsController : BaseController
    {
        // GET: Subscriptions
        public async Task<ActionResult> Index()
        {
            var subscriptions = Db.Subscriptions.Include(s => s.Company).Include(s => s.Plan).Include(s => s.User).OrderBy(u => u.User.FirstName);
            return View(await subscriptions.ToListAsync());
        }

        // GET: Subscriptions/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscription subscription = await Db.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return HttpNotFound();
            }
            return View(subscription);
        }

        // GET: Subscriptions/Create
        public ActionResult Create()
        {
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId");
            ViewBag.PlanId = new SelectList(Db.SubscriptionPlans, "Id", "Title");
            ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName");
            return View();
        }

        // POST: Subscriptions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,ActiveUntil,Notes,CompanyId,UserId,PlanId,CreatedDate")] Subscription subscription)
        {
            if (ModelState.IsValid)
            {
                subscription.CreatedDate = DateTime.Now;

                Db.Subscriptions.Add(subscription);
                await Db.SaveChangesAsync();
                
                return RedirectToAction("Index");
            }

            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", subscription.CompanyId);
            ViewBag.PlanId = new SelectList(Db.SubscriptionPlans, "Id", "Title", subscription.PlanId);
            ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", subscription.UserId);
            return View(subscription);
        }

        // GET: Subscriptions/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscription subscription = await Db.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return HttpNotFound();
            }
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", subscription.CompanyId);
            ViewBag.PlanId = new SelectList(Db.SubscriptionPlans, "Id", "Title", subscription.PlanId);
            ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", subscription.UserId);
            return View(subscription);
        }

        // POST: Subscriptions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,ActiveUntil,Notes,CompanyId,UserId,PlanId,CreatedDate")] Subscription subscription)
        {
            if (ModelState.IsValid)
            {
                Db.Entry(subscription).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.CompanyId = new SelectList(Db.Companies, "Id", "CreatorId", subscription.CompanyId);
            ViewBag.PlanId = new SelectList(Db.SubscriptionPlans, "Id", "Title", subscription.PlanId);
            ViewBag.UserId = new SelectList(Db.Users, "Id", "FirstName", subscription.UserId);
            return View(subscription);
        }

        // GET: Subscriptions/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscription subscription = await Db.Subscriptions.FindAsync(id);
            if (subscription == null)
            {
                return HttpNotFound();
            }
            return View(subscription);
        }

        // POST: Subscriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Subscription subscription = await Db.Subscriptions.FindAsync(id);
            Db.Subscriptions.Remove(subscription);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
