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
        #region Methods
        private async Task<bool> IsActivityFromCurrentCompany(Activity activity)
        {
            // Pull out the current company to compare to the activity
            Member member = await GetCurrentMemberAsync();
            Company currentCompany = member.Company;
            bool isActivitySafe = activity.CompanyId == currentCompany.Id;
            return isActivitySafe;
        }

        private async Task<Activity> FindSafeActivity(int id)
        {
            // Pull the activity from the database
            Activity activity = await Db.Activities.FindAsync(id);

            // Ensure it exists
            if (activity == null)
            {
                return null;
            }

            bool isActivitySafe = await IsActivityFromCurrentCompany(activity);

            // If this activity it not for the current company
            if (!isActivitySafe)
            {
                return null;
            }

            return activity;
        }

        private async Task LoadFeedActivities()
        {
            Member member = await GetCurrentMemberAsync();
            Company company = member.Company;
            int companyId = company.Id;

            ViewBag.FeedActivities = await Db.Activities.WhereCompany(companyId).ToArrayAsync();
        }

        #endregion

        // GET: Activities
        public async Task<ActionResult> Index()
        {
            await LoadFeedActivities();

            return View();
        }

        // POST: Activities/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index([Bind(Include = "Title,Description")] Activity activity)
        {
            if (ModelState.IsValid)
            {
                // Pull out the relevant data to the context
                ApplicationUser user = await GetCurrentUserAsync();
                Member member = await GetCurrentMemberAsync();

                // Setup the new activity and save
                Activity newActivity = Db.Activities.Create(user, member.Company, activity.Title);
                newActivity.Description = activity.Description;
                newActivity.Type = ActivityType.TeamNotification;
                await Db.SaveChangesAsync();

                // Return to the list action for this 
                return RedirectToAction("Index");
            }

            await LoadFeedActivities();

            return View(activity);
        }

        // GET: Activities/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            // Pull the activity from the database
            Activity activity = await FindSafeActivity(id);

            // Ensure it exists
            if (activity == null)
            {
                return HttpNotFound();
            }

            // Send down the edit form
            return View(activity);
        }

        // POST: Activities/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Description")] Activity activityViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(activityViewModel);
            }

            // Pull the activity from the database
            Activity activity = await FindSafeActivity(activityViewModel.Id);

            // Ensure it exists
            if (activity == null)
            {
                return HttpNotFound();
            }

            activity.Title = activityViewModel.Title;
            activity.Description = activityViewModel.Description;
            Db.Entry(activity).State = EntityState.Modified;

            await Db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Activities/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            // Pull the activity from the database
            Activity activity = await FindSafeActivity(id);

            // Ensure it exists
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
            // Pull the activity from the database
            Activity activity = await FindSafeActivity(id);

            // Ensure it exists
            if (activity == null)
            {
                return HttpNotFound();
            }

            Db.Activities.Remove(activity);

            await Db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// GET action for the activities widget
        /// </summary>
        /// <returns></returns>
        public ActionResult Widget()
        {
            Member member = GetCurrentMember();

            if(member != null)
            {
                Company company = member.Company;
                int companyId = company.Id;
                IEnumerable<Activity> activities = Db.Activities.WhereCompany(companyId).Take(5).ToArray();
                return View(activities);
            }
            else
            {
                return null;
            }           
        }
    }
}
