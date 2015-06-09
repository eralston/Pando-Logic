using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

using Masticore;

using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    [NotMapped]
    public class GoalViewModel : Goal
    {
        const string DateFormat = "MM/dd/yyyy";

        public GoalViewModel() { }

        public GoalViewModel(Goal goal)
        {
            this.Id = goal.Id;
            this.Title = goal.Title;
            this.Description = goal.Description;
            this.Color = goal.Color;
            this.Icon = goal.Icon;
            if (goal.DueDateUtc.HasValue)
                this.DueDateString = goal.DueDateUtc.Value.ToString("d");
        }

        [Display(Name = "Due Date")]
        [DateTimeStringValidation(Format = DateFormat)]
        public string DueDateString { get; set; }

        public bool HasDueDate()
        {
            return !string.IsNullOrEmpty(DueDateString);
        }

        public DateTime ParsedDueDateTime()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            return DateTime.ParseExact(DueDateString, DateFormat, provider);
        }
    }

    public class GoalsController : BaseController
    {
        #region Methods

        private async Task ApplyGoalToViewBag(Goal goal)
        {
            IQueryable<WorkItem> query = Db.WorkItems.WhereGoal(goal.Id).OrderBy(t => t.DueDateUtc);

            ViewBag.Tasks = await query.ToArrayAsync();
            ViewBag.GoalId = goal.Id;
            ViewBag.IsMyGoal = goal.UserId == UserCache.Id;

            ActivityRepository repo = await GetActivityRepositoryForCurrentCompany();
            if (repo != null)
            {
                await goal.LoadComments<Goal>(this, "CreateGoal", repo);
            }
        }

        private IQueryable<Goal> BuildGoalQuery(bool limited, Member currentMember)
        {
            IQueryable<Goal> query = Db.Goals.WhereMember(currentMember).OrderBy(g => g.DueDateUtc).Include(g => g.WorkItems);

            if (limited)
            {
                query = query.Take(5);

                ViewBag.GoalBoxShowAll = true;
                ViewBag.GoalBoxShowAllUrl = Url.Action("Index", "Goals");
            }

            return query;
        }

        private Goal[] QueryActiveGoals(bool limited = true)
        {
            Member currentMember = GetCurrentMember();
            if (currentMember == null)
            {
                return null;
            }

            IQueryable<Goal> query = BuildGoalQuery(limited, currentMember);
            query = query.Where(g => g.ArchiveDateUtc == null);
            var goals = query.ToArray();
            return goals;
        }

        #endregion

        // GET: Goals
        public async Task<ActionResult> Index()
        {
            Member currentMember = await GetCurrentMemberAsync();

            IQueryable<Goal> query = BuildGoalQuery(false, currentMember);
            ;

            // filter on querystring logic 
            var showAll = (Request.QueryString["ShowAll"] ?? "").ToUpper() == "TRUE";

            if (!showAll)
            {
                query = query.Where(g => g.ArchiveDateUtc == null);
                ViewBag.GoalBoxShowAll = true;
                ViewBag.GoalBoxShowAllUrl = Url.Action("Index", new { ShowAll = true });
                ViewBag.GoalBoxShowAllTitle = "Show Archived";
            }
            else
            {
                ViewBag.GoalBoxShowAll = true;
                ViewBag.GoalBoxShowAllUrl = Url.Action("Index");
                ViewBag.GoalBoxShowAllTitle = "Hide Archived";
            }

            Goal[] goals = await query.ToArrayAsync();

            goals = (from g in goals
                     orderby
                         g.ArchiveDateUtc.HasValue, g.ArchiveDateUtc,
                         g.DueDateUtc.HasValue descending, g.DueDateUtc
                     select g).ToArray();

            return View(goals);
        }

        // GET: Goals/Details/5
        public async Task<ActionResult> Details(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }

            ViewBag.GoalProgress = goal.CalculateProgress();

            await ApplyGoalToViewBag(goal);

            return View(goal);
        }

        // GET: Goals/Tasks/5
        /// <summary>
        /// Pulls a screen that lists all of the tasks for a given goal ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Tasks(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }

            ViewBag.TaskBoxTitle = "Goal Tasks";

            await ApplyGoalToViewBag(goal);

            return View(goal);
        }

        // GET: Goals/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Goals/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DueDateString,Title,Description,Color,Icon")] GoalViewModel goalViewModel)
        {
            if (ModelState.IsValid)
            {
                Goal goal = Db.Goals.Create();

                Member currentMember = await GetCurrentMemberAsync();

                if (goalViewModel.HasDueDate())
                    goal.DueDateUtc = goalViewModel.ParsedDueDateTime();

                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

                goal.CreatedDateUtc = DateTime.UtcNow;
                goal.CompanyId = currentMember.CompanyId;
                goal.UserId = currentMember.UserId;

                goal.Color = goalViewModel.Color;
                goal.Icon = goalViewModel.Icon;

                Db.Goals.Add(goal);

                // Add an activity model
                Activity newActivity = new Activity(currentMember.UserId, "");
                newActivity.CompanyId = goal.CompanyId.Value;
                newActivity.SetTitle(goal.Title, Url.Action("Details", "Goals", new { id = goal.Id }));
                newActivity.Description = "Goal Created";
                newActivity.Type = ActivityType.WorkAdded;
                ActivityRepository repo = ActivityRepository.CreateForCompany(goal.CompanyId.Value);
                await repo.InsertOrReplace<Goal>(goal.Id, newActivity);

                await Db.SaveChangesAsync();

                await UpdateCurrentUserCacheGoalsAsync();

                return RedirectToAction("Index");
            }

            return View(goalViewModel);
        }

        // GET: Goals/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Goal goal = await Db.Goals.FindAsync(id);

            if (goal == null)
            {
                return HttpNotFound();
            }

            GoalViewModel vm = new GoalViewModel(goal);

            return View(vm);
        }

        // POST: Goals/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,DueDateString,Title,Description,Color,Icon")] GoalViewModel goalViewModel)
        {
            if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalViewModel.Id);

                if (goalViewModel.HasDueDate())
                    goal.DueDateUtc = goalViewModel.ParsedDueDateTime();

                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

                goal.Color = goalViewModel.Color;
                goal.Icon = goalViewModel.Icon;

                Db.Entry(goal).State = EntityState.Modified;


                await Db.SaveChangesAsync();

                await UpdateCurrentUserCacheGoalsAsync();

                return RedirectToAction("Details", new { id = goal.Id });
            }

            return View(goalViewModel);
        }

        // GET: Goals/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Goal goals = await Db.Goals.FindAsync(id);
            if (goals == null)
            {
                return HttpNotFound();
            }
            return View(goals);
        }

        // POST: Goals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            // Find the goal
            Goal goal = await Db.Goals.FindAsync(id);

            // Add an activity model
            Member currentMember = await GetCurrentMemberAsync();
            Activity newActivity = new Activity(currentMember.UserId, goal.Title);
            newActivity.CompanyId = currentMember.CompanyId;
            newActivity.Description = "Goal Deleted";
            newActivity.Type = ActivityType.WorkDeleted;
            ActivityRepository repo = ActivityRepository.CreateForCompany(goal.CompanyId.Value);
            await repo.InsertOrReplace<Goal>(goal.Id, newActivity);

            // Remove the goal
            await Db.RemoveWorkItemsForGoal(goal.Id);
            Db.Goals.Remove(goal);
            await Db.SaveChangesAsync();

            await UpdateCurrentUserCacheGoalsAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Preview widget for the dashboard
        /// </summary>
        /// <returns></returns>
        public ActionResult Widget()
        {
            Member currentMember = GetCurrentMember();
            if (currentMember == null)
                return new EmptyResult();

            Goal[] goals = this.UserCache.Goals;

            if (goals == null)
                return new EmptyResult();

            return View(goals);
        }

        public ActionResult WidgetBox()
        {
            var goals = QueryActiveGoals(false);

            if (goals == null)
                return new EmptyResult();

            return View(goals);
        }

        /// <summary>
        /// Archives the goal with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Archive(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }
            return View(goal);
        }

        [HttpPost, ActionName("Archive")]
        public async Task<ActionResult> ArchiveConfirmed(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }

            goal.Archive();

            // Setup the new activity and save
            Member member = await GetCurrentMemberAsync();
            Activity newActivity = new Activity(member.UserId, "");
            newActivity.CompanyId = goal.CompanyId.Value;
            newActivity.SetTitle(goal.Title, Url.Action("Details", "Goals", new { id = goal.Id }));
            newActivity.Description = "Goal Archived";
            newActivity.Type = ActivityType.WorkArchived;
            ActivityRepository repo = ActivityRepository.CreateForCompany(goal.CompanyId.Value);
            await repo.InsertOrReplace<Goal>(goal.Id, newActivity);

            await Db.SaveChangesAsync();

            await UpdateCurrentUserCacheGoalsAsync();

            return RedirectToAction("Index", "Goals");
        }

        public async Task<ActionResult> UndoArchive(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }
            return View(goal);
        }

        [HttpPost, ActionName("UndoArchive")]
        public async Task<ActionResult> UndoArchiveConfirmed(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            if (goal == null)
            {
                return HttpNotFound();
            }

            goal.UndoArchive();

            // Setup the new activity and save
            Member member = await GetCurrentMemberAsync();
            Activity newActivity = new Activity(member.UserId, "");
            newActivity.CompanyId = goal.CompanyId.Value;
            newActivity.SetTitle(goal.Title, Url.Action("Details", "Goals", new { id = goal.Id }));
            newActivity.Description = "Goal Unarchived";
            newActivity.Type = ActivityType.WorkUndoArchived;
            ActivityRepository repo = ActivityRepository.CreateForCompany(goal.CompanyId.Value);
            await repo.InsertOrReplace<Goal>(goal.Id, newActivity);

            await Db.SaveChangesAsync();

            await UpdateCurrentUserCacheGoalsAsync();

            return RedirectToAction("Details", new { id = id });
        }
    }
}
