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
            if (goal.DueDate.HasValue)
                this.DueDateString = goal.DueDate.Value.ToString("d");
        }

        [Required]
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

        private async Task ApplyGoalToViewBag(Goal goal, bool limited = true)
        {
            IQueryable<WorkItem> query = Db.WorkItems.WhereGoal(goal.Id).OrderBy(t => t.DueDate);

            if (limited)
            {
                query = query.Take(5).Where(w => w.CompletedDate == null);
            }

            ViewBag.Tasks = await query.ToArrayAsync();
            ViewBag.GoalId = goal.Id;
            ViewBag.IsMyGoal = goal.CreatorId == UserCache.Id;

            if (limited)
            {
                ViewBag.TaskBoxShowAll = true;
                ViewBag.TaskBoxShowAllUrl = Url.Action("Tasks", "Goals", new { id = goal.Id });
                goal.LoadComments(this, "CreateGoal");
            }
        }

        private IQueryable<Goal> BuildGoalQuery(bool limited, Member currentMember)
        {
            IQueryable<Goal> query = Db.Goals.WhereMember(currentMember).OrderBy(g => g.DueDate).Include(g => g.WorkItems);

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
            IQueryable<Goal> query = BuildGoalQuery(limited, currentMember);
            query = query.Where(g => g.ArchiveDate == null);
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
                query = query.Where(g => g.ArchiveDate == null);
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

            var goals = await query.ToArrayAsync();

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

            await ApplyGoalToViewBag(goal, false);

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

                goal.DueDate = goalViewModel.ParsedDueDateTime();
                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

                goal.CreatedDate = DateTime.Now;
                goal.CompanyId = currentMember.CompanyId;
                goal.CreatorId = currentMember.UserId;

                goal.Color = goalViewModel.Color;
                goal.Icon = goalViewModel.Icon;

                Db.Goals.Add(goal);

                // Add an activity model
                Activity newActivity = Db.Activities.Create(currentMember.UserId, currentMember.Company, goal.Title);
                newActivity.Description = goal.Description;
                newActivity.Type = ActivityType.WorkAdded;

                await Db.SaveChangesAsync();

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

                goal.DueDate = goalViewModel.ParsedDueDateTime();
                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

                goal.Color = goalViewModel.Color;
                goal.Icon = goalViewModel.Icon;

                Db.Entry(goal).State = EntityState.Modified;
                await Db.SaveChangesAsync();
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
            // Remove the goal
            Goal goal = await Db.Goals.FindAsync(id);
            Db.Goals.Remove(goal);

            await Db.RemoveWorkItemsForGoal(goal.Id);

            // Add an activity model
            Member currentMember = await GetCurrentMemberAsync();
            Activity newActivity = Db.Activities.Create(currentMember.UserId, currentMember.Company, goal.Title);
            newActivity.Description = goal.Description;
            newActivity.Type = ActivityType.WorkDeleted;

            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Preview widget for the dashboard
        /// </summary>
        /// <returns></returns>
        public ActionResult Widget()
        {
            var goals = QueryActiveGoals();
            return View(goals);
        }

        public ActionResult WidgetBox()
        {
            var goals = QueryActiveGoals(false);
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
            Activity newActivity = Db.Activities.Create(member.UserId, member.Company, goal.Title);
            newActivity.Description = goal.Description;
            newActivity.Type = ActivityType.WorkArchived;

            await Db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
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
            Activity newActivity = Db.Activities.Create(member.UserId, member.Company, goal.Title);
            newActivity.Description = goal.Description;
            newActivity.Type = ActivityType.WorkUndoArchived;

            await Db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = id });
        }
    }
}
