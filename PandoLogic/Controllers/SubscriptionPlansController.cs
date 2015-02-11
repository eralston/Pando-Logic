using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;

using Stripe;

using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    [AdminAuthorize]
    public class SubscriptionPlansController : BaseController
    {
        // GET: SubscriptionPlans
        public async Task<ActionResult> Index()
        {
            return View(await Db.SubscriptionPlans.ToListAsync());
        }

        // GET: SubscriptionPlans/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SubscriptionPlan subscriptionPlan = await Db.SubscriptionPlans.FindAsync(id);
            if (subscriptionPlan == null)
            {
                return HttpNotFound();
            }
            return View(subscriptionPlan);
        }

        // GET: SubscriptionPlans/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SubscriptionPlans/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Title,Note,Description,Price,State,CreatedDate,TrialDays")] SubscriptionPlan subscriptionPlan)
        {
            if (ModelState.IsValid)
            {
                subscriptionPlan.PaymentSystemId = Guid.NewGuid().ToString();

                StripeManager.CreatePlan(subscriptionPlan);

                // Save to our database
                subscriptionPlan.CreatedDateUtc = DateTime.UtcNow;
                Db.SubscriptionPlans.Add(subscriptionPlan);
                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(subscriptionPlan);
        }

        // GET: SubscriptionPlans/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SubscriptionPlan subscriptionPlan = await Db.SubscriptionPlans.FindAsync(id);
            if (subscriptionPlan == null)
            {
                return HttpNotFound();
            }
            return View(subscriptionPlan);
        }

        // POST: SubscriptionPlans/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Note,Description,State")] SubscriptionPlan subscriptionPlan)
        {
            if (ModelState.IsValid)
            {
                // Pull out the original
                SubscriptionPlan plan = await Db.SubscriptionPlans.FindAsync(subscriptionPlan.Id);

                // Save editable fields
                plan.Description = subscriptionPlan.Description;
                plan.Note = subscriptionPlan.Note;
                plan.State = subscriptionPlan.State;
                plan.Title = subscriptionPlan.Title;

                // Update Stripe
                StripeManager.UpdatePlan(plan);

                // Save changes to the database
                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(subscriptionPlan);
        }

        // GET: SubscriptionPlans/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SubscriptionPlan subscriptionPlan = await Db.SubscriptionPlans.FindAsync(id);
            if (subscriptionPlan == null)
            {
                return HttpNotFound();
            }
            return View(subscriptionPlan);
        }

        // POST: SubscriptionPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            SubscriptionPlan subscriptionPlan = await Db.SubscriptionPlans.FindAsync(id);

            // Remove from Stripe
            StripeManager.DeletePlan(subscriptionPlan);

            Db.SubscriptionPlans.Remove(subscriptionPlan);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
