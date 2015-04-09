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
            bool isActivitySafe = activity.CompanyId == member.CompanyId;
            return isActivitySafe;
        }

        //private async Task<Activity> FindSafeActivity(int companyId, string rowKey)
        //{
        //    // Pull the activity from the database
        //    ActivityRepository repo = ActivityRepository.CreateForCompany(companyId);

        //    Activity activity = await repo.RetrieveComment<

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return null;
        //    }

        //    bool isActivitySafe = await IsActivityFromCurrentCompany(activity);

        //    // If this activity it not for the current company
        //    if (!isActivitySafe)
        //    {
        //        return null;
        //    }

        //    return activity;
        //}

        public static async Task LoadFeedActivities(BaseController controller, int? limit = null)
        {
            Member member = await controller.GetCurrentMemberAsync();
            int? companyId = null;

            if (member != null)
            {
                Company company = member.Company;
                companyId = company.Id;
            }

            // TODO: Optimize this to NOT pull the whole table into memory
            ActivityRepository repo = await controller.GetActivityRepositoryForCurrentCompany();
            var activities = await repo.RetrieveAll();
            IEnumerable<Activity> sort = activities.OrderByDescending(a => a.Timestamp);
            if(limit.HasValue)
                sort = sort.Take(limit.Value);

            controller.TempData["Activities"] = sort;
        }

        //private async Task<ActionResult> Delete(Activity activity, ActionResult result)
        //{
        //    Db.Activities.Remove(activity);

        //    await Db.SaveChangesAsync();

        //    return result;
        //}

        //private ActionResult ResultForActivity(Activity activity)
        //{
        //    if (activity.GoalId.HasValue)
        //    {
        //        return RedirectToAction("Details", "Goals", new { id = activity.GoalId.Value });
        //    }
        //    else if (activity.WorkItemId.HasValue)
        //    {
        //        return RedirectToAction("Details", "Tasks", new { id = activity.WorkItemId.Value });
        //    }
        //    else
        //    {
        //        return RedirectToAction("Index");
        //    }
        //}

        //private async Task EditActivity(Activity activityViewModel, Activity activity)
        //{
        //    activity.Title = activityViewModel.Title;
        //    activity.Description = activityViewModel.Description;
        //    Db.Entry(activity).State = EntityState.Modified;

        //    await Db.SaveChangesAsync();
        //}

        #endregion

        // GET: Activities
        public async Task<ActionResult> Index()
        {
            await LoadFeedActivities(this);

            ViewBag.IsFromActivityFeed = true;
            ViewBag.ShowActivityTitle = true;

            ViewBag.FeedActivities = TempData["Activities"] as IEnumerable<Activity>;

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
                Member member = await GetCurrentMemberAsync();

                // Setup the new activity and save
                string title = string.Format("Team Notification for {0}: {1}", member.Company.Name, activity.Title);

                Activity newActivity = new Activity(member.UserId, title);
                newActivity.CompanyId = member.CompanyId;
                newActivity.Description = activity.Description;
                newActivity.Type = ActivityType.TeamNotification;
                ActivityRepository repo = await GetActivityRepositoryForCurrentCompany();
                await repo.InsertOrUpdate(newActivity);

                // Return to the list action for this 
                return RedirectToAction("Index");
            }

            await LoadFeedActivities(this);

            return View(activity);
        }

        // GET: Activities/Edit/5
        //public async Task<ActionResult> EditActivity(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ViewBag.IsFromActivityFeed = true;

        //    // Send down the edit form
        //    return View("Edit", activity);
        //}

        //// POST: Activities/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> EditActivity([Bind(Include = "Id,Title,Description")] Activity activityViewModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.IsFromActivityFeed = true;
        //        return View("Edit", activityViewModel);
        //    }

        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(activityViewModel.Id);
        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ActionResult result = RedirectToAction("Index");
        //    await EditActivity(activityViewModel, activity);
        //    return result;
        //}

        //// GET: Activities/Edit/5
        //public async Task<ActionResult> Edit(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    // Send down the edit form
        //    return View(activity);
        //}

        //// POST: Activities/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Description")] Activity activityViewModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(activityViewModel);
        //    }

        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(activityViewModel.Id);
        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ActionResult result = ResultForActivity(activity);
        //    await EditActivity(activityViewModel, activity);
        //    return result;
        //}

        //// GET: Activities/DeleteActivity/5
        //public async Task<ActionResult> DeleteActivity(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ViewBag.IsFromActivityFeed = true;

        //    return View("Delete", activity);
        //}

        //// POST: Activities/DeleteActivity/5
        //[HttpPost, ActionName("DeleteActivity")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteActivityConfirmed(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);
        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ActionResult result = RedirectToAction("Index");

        //    return await Delete(activity, result);
        //}

        //// GET: Activities/Delete/5
        //public async Task<ActionResult> Delete(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    return View("Delete", activity);
        //}

        //// POST: Activities/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteConfirmed(int id)
        //{
        //    // Pull the activity from the database
        //    Activity activity = await FindSafeActivity(id);

        //    // Ensure it exists
        //    if (activity == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    ActionResult result = ResultForActivity(activity);

        //    return await Delete(activity, result);
        //}

        /// <summary>
        /// GET action for the activities widget
        /// </summary>
        /// <returns></returns>
        public ActionResult Widget()
        {
            int? companyId = null;
            Member member = GetCurrentMember();
            if (member != null)
            {
                Company company = member.Company;
                companyId = company.Id;
            }

            ViewBag.ShowActivityTitle = true;

            var activities = this.TempData["Activities"] as IEnumerable<Activity>;

            return View(activities);
        }
    }
}
