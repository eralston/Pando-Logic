using PandoLogic.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using Masticore;

namespace PandoLogic.Controllers
{
    [NotMapped]
    public class WorkItemViewModel : WorkItem
    {
        const string DateFormat = "MM/dd/yyyy";

        public WorkItemViewModel() { }

        public WorkItemViewModel(WorkItem workItem)
        {
            this.Id = workItem.Id;
            this.Title = workItem.Title;
            this.Description = workItem.Description;
            if (workItem.DueDateUtc.HasValue)
                this.DueDateString = workItem.DueDateUtc.Value.ToString("d");
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

    [RoutePrefix("Tasks")]
    public class WorkItemsController : BaseController
    {
        #region Methods

        private async Task LoadAssigneeOptions(string id = null)
        {
            int companyId = UserCache.SelectedCompanyId;
            ApplicationUser[] users = await Db.UsersInCompany(companyId).ToArrayAsync();

            if (!string.IsNullOrEmpty(id))
            {
                ViewBag.AssigneeId = new SelectList(users, "Id", "FullName", id);
            }
            else
            {
                ViewBag.AssigneeId = new SelectList(users, "Id", "FullName");
            }
        }

        #endregion

        /// <summary>
        /// Gets all tasks for the current company (no ID)
        /// OR all tasks for the goal with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("")]
        [Route("{id}")]
        public async Task<ActionResult> Index(int? id)
        {
            IQueryable<WorkItem> query = null;
            if (id.HasValue)
            {
                Goal goal = await Db.Goals.FindAsync(id.Value);
                if (goal.CompanyId != UserCache.SelectedCompanyId)
                    return RedirectToAction("Index");

                ViewBag.GoalId = id;
                query = Db.WorkItems.WhereGoal(id.Value).OrderBy(w => w.DueDateUtc);
            }
            else
            {
                int companyId = UserCache.SelectedCompanyId;
                query = Db.WorkItems.WhereCompany(companyId).OrderBy(w => w.DueDateUtc);
            }

            // filter on querystring logic 
            var showAll = (Request.QueryString["ShowAll"] ?? "").ToUpper() == "FALSE";

            if (!showAll)
            {
                query = query.Where(w => w.CompletedDateUtc == null);
                ViewBag.TaskBoxShowAll = true;
                ViewBag.TaskBoxShowAllUrl = Url.Action("Index", new { ShowAll = true });
                ViewBag.TaskBoxShowAllTitle = "Show Completed";
            }
            else
            {
                ViewBag.TaskBoxShowAll = true;
                ViewBag.TaskBoxShowAllUrl = Url.Action("Index");
                ViewBag.TaskBoxShowAllTitle = "Hide Completed";
            }

            WorkItem[] workItems = await query.ToArrayAsync();

            return View(workItems);
        }

        // GET: WorkItems/Details/5
        [Route("Details/{id}")]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            WorkItem workItem = await Db.WorkItems.FindAsync(id);

            if (workItem == null)
            {
                return HttpNotFound();
            }

            ViewBag.IsMyTask = workItem.AssigneeId == UserCache.Id || workItem.AssigneeId == null;

            ActivityRepository repo = ActivityRepository.CreateForCompany(workItem.CompanyId.Value);
            await workItem.LoadComments(this, "CreateTask", repo);
            UnstashModelState();

            return View(workItem);
        }

        // GET: WorkItems/Create
        /// <summary>
        /// Creates a new task
        /// If id == null, then this is not assigned to a goal
        /// Otherwise, it is assigned to a goal
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Create")]
        [Route("Create/{id}")]
        public async Task<ActionResult> Create(int? id)
        {
            await LoadAssigneeOptions();

            if (id.HasValue)
            {
                ViewBag.Goal = await Db.Goals.FindAsync(id.Value);
            }

            return View();
        }

        [HttpPost]
        [Route("Complete/{id}")]
        public async Task<ActionResult> Complete(int id)
        {
            // Find the work item and complete it in the db
            WorkItem workItem = await Db.WorkItems.FindAsync(id);
            workItem.CompletedDateUtc = DateTime.UtcNow;
            await Db.SaveChangesAsync();

            Member currentMember = await GetCurrentMemberAsync();

            // Save activity for this completion
            Activity newActivity = new Activity(currentMember.UserId, workItem.Title);
            newActivity.CompanyId = currentMember.CompanyId;
            newActivity.SetTitle(workItem.Title, Url.Action("Details", "Tasks", new { id = workItem.Id }));
            newActivity.Description = "Task Completed";
            newActivity.Type = ActivityType.WorkCompleted;
            ActivityRepository repo = ActivityRepository.CreateForCompany(currentMember.CompanyId);
            await repo.InsertOrUpdate<WorkItem>(workItem.Id, newActivity);
            
            // Update goal cache to reflect progress
            await UpdateCurrentUserCacheGoalsAsync();

            return new HttpStatusCodeResult(200);
        }

        [HttpPost]
        [Route("Uncomplete/{id}")]
        public async Task<ActionResult> Uncomplete(int id)
        {
            WorkItem workItem = await Db.WorkItems.FindAsync(id);
            workItem.CompletedDateUtc = null;
            await Db.SaveChangesAsync();

            Member currentMember = await GetCurrentMemberAsync();

            // Save activity for the undo of completion
            Activity newActivity = new Activity(currentMember.UserId, workItem.Title);
            newActivity.CompanyId = workItem.CompanyId.Value;
            newActivity.SetTitle(workItem.Title, Url.Action("Details", "Tasks", new { id = workItem.Id }));
            newActivity.Description = "Undo Task Completed";
            newActivity.Type = ActivityType.WorkUndoArchived;
            ActivityRepository repo = ActivityRepository.CreateForCompany(workItem.CompanyId.Value);
            await repo.InsertOrUpdate<WorkItem>(workItem.Id, newActivity);

            await UpdateCurrentUserCacheGoalsAsync();

            return new HttpStatusCodeResult(200);
        }

        // POST: WorkItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Create")]
        [Route("Create/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(int? id, [Bind(Include = "AssigneeId,EstimatedTime,DueDateString,Title,Description,GoalId")] WorkItemViewModel workItemViewModel)
        {
            if (ModelState.IsValid)
            {
                WorkItem workItem = Db.WorkItems.Create();

                Member currentMember = await GetCurrentMemberAsync();

                workItem.CreatedDateUtc = DateTime.UtcNow;
                workItem.CompanyId = currentMember.CompanyId;
                workItem.UserId = currentMember.UserId;

                workItem.CompletedDateUtc = null;
                workItem.Title = workItemViewModel.Title;
                workItem.Description = workItemViewModel.Description;
                workItem.AssigneeId = workItemViewModel.AssigneeId;
                workItem.EstimatedTime = workItemViewModel.EstimatedTime;

                if (workItemViewModel.HasDueDate())
                    workItem.DueDateUtc = workItemViewModel.ParsedDueDateTime();

                if (id.HasValue)
                {
                    // TODO: Validate this goal ID is appropo to the user
                    Goal goal = await Db.Goals.FindAsync(id.Value);
                    workItem.Goal = goal;

                    goal.WorkItems.Add(workItem);
                }

                Db.WorkItems.Add(workItem);

                await Db.SaveChangesAsync();

                // Add an activity model
                Activity newActivity = new Activity(currentMember.UserId, workItem.Title);
                newActivity.CompanyId = workItem.CompanyId.Value;
                newActivity.SetTitle(workItem.Title, Url.Action("Details", "Tasks", new { id = workItem.Id }));
                newActivity.Description = "Task Created";
                newActivity.Type = ActivityType.WorkAdded;
                ActivityRepository repo = ActivityRepository.CreateForCompany(workItem.CompanyId.Value);
                await repo.InsertOrUpdate<WorkItem>(workItem.Id, newActivity);

                if (id.HasValue)
                {
                    return RedirectToAction("Details", "Goals", new { id = id.Value });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            await LoadAssigneeOptions(workItemViewModel.AssigneeId);

            return View(workItemViewModel);
        }

        // GET: WorkItems/Edit/5
        [Route("Edit/{id}")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WorkItem workItem = await Db.WorkItems.FindAsync(id);
            if (workItem == null)
            {
                return HttpNotFound();
            }

            WorkItemViewModel viewModel = new WorkItemViewModel(workItem);

            await LoadAssigneeOptions(workItem.AssigneeId);
            return View(viewModel);
        }

        // POST: WorkItems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Edit/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,AssigneeId,EstimatedTime,DueDateString,Title,Description")] WorkItemViewModel workItemViewModel)
        {
            if (ModelState.IsValid)
            {
                WorkItem workItem = await Db.WorkItems.FindAsync(workItemViewModel.Id);

                workItem.Title = workItemViewModel.Title;
                workItem.Description = workItemViewModel.Description;
                workItem.EstimatedTime = workItemViewModel.EstimatedTime;

                if(workItemViewModel.HasDueDate())
                    workItem.DueDateUtc = workItemViewModel.ParsedDueDateTime();                

                Db.Entry(workItem).State = EntityState.Modified;
                await Db.SaveChangesAsync();

                return RedirectToAction("Details", new { id = workItem.Id });
            }

            await LoadAssigneeOptions(workItemViewModel.AssigneeId);
            return View(workItemViewModel);
        }

        // GET: WorkItems/Delete/5
        [Route("Delete/{id}")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WorkItem workItem = await Db.WorkItems.FindAsync(id);
            if (workItem == null)
            {
                return HttpNotFound();
            }
            return View(workItem);
        }

        // POST: WorkItems/Delete/5
        [Route("Delete/{id}")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            // Find the workitem
            WorkItem workItem = await Db.WorkItems.FindAsync(id);

            // Add an activity model
            Member currentMember = await GetCurrentMemberAsync();
            Activity newActivity = new Activity(currentMember.UserId, workItem.Title);
            newActivity.CompanyId = workItem.CompanyId.Value;
            newActivity.Description = "Task Deleted";
            newActivity.Type = ActivityType.WorkDeleted;
            ActivityRepository repo = ActivityRepository.CreateForCompany(workItem.CompanyId.Value);
            await repo.InsertOrUpdate<WorkItem>(workItem.Id, newActivity);

            // Remove it
            int? goalId = workItem.GoalId;
            Db.WorkItems.Remove(workItem);
            await Db.SaveChangesAsync();

            if (goalId.HasValue)
            {
                return RedirectToAction("Details", "Goals", new { id = goalId.Value });
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Gets all tasks for the current company (no ID)
        /// OR all tasks for the goal with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Widget")]
        public ActionResult Widget()
        {
            Member currentMember = GetCurrentMember();
            if (currentMember == null)
                return new EmptyResult();

            WorkItem[] workItems = Db.WorkItems.WhereAssignedUser(currentMember.UserId).Where(w => w.CompletedDateUtc == null).OrderBy(w => w.DueDateUtc).Take(5).ToArray();

            ViewBag.TaskBoxShowAll = true;
            ViewBag.TaskBoxShowAllUrl = Url.Action("Index");

            return View(workItems);
        }
    }
}
