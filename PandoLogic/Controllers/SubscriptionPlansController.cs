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
        #region Methods

        /// <summary>
        /// Creates a new plan inside of Stripe, using the given subscription plan's information
        /// </summary>
        /// <param name="subscriptionPlan"></param>
        private static void CreatePlanInStripe(SubscriptionPlan subscriptionPlan)
        {
            // Save it to Stripe
            StripePlanCreateOptions newStripePlan = new StripePlanCreateOptions();
            newStripePlan.Amount = Convert.ToInt32(subscriptionPlan.Price * 100.0);           // all amounts on Stripe are in cents, pence, etc
            newStripePlan.Currency = "usd";        // "usd" only supported right now
            newStripePlan.Interval = "month";      // "month" or "year"
            newStripePlan.IntervalCount = 1;       // optional
            newStripePlan.Name = subscriptionPlan.Title;
            newStripePlan.TrialPeriodDays = 14;    // amount of time that will lapse before the customer is billed
            newStripePlan.Id = subscriptionPlan.Identifier;

            StripePlanService planService = new StripePlanService();
            planService.Create(newStripePlan);
        }

        private static void UpdatePlanInStripe(SubscriptionPlan plan)
        {
            StripePlanUpdateOptions options = new StripePlanUpdateOptions();
            options.Name = plan.Title;

            StripePlanService planService = new StripePlanService();
            planService.Update(plan.Identifier, options);
        }

        #endregion

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
                subscriptionPlan.Identifier = Guid.NewGuid().ToString();

                CreatePlanInStripe(subscriptionPlan);

                // Save to our database
                subscriptionPlan.CreatedDate = DateTime.Now;
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
                UpdatePlanInStripe(plan);

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
            Db.SubscriptionPlans.Remove(subscriptionPlan);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
