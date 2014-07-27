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
            this.DueDateString = workItem.DueDate.Value.ToString("d");
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
        [Route("{id}")]
        public async Task<ActionResult> Index(int? id)
        {
            WorkItem[] workItems = null;
            if (id.HasValue)
            {
                Goal goal = await Db.Goals.FindAsync(id.Value);
                if (goal.CompanyId != UserCache.SelectedCompanyId)
                    return RedirectToAction("Index");

                ViewBag.GoalId = id;
                workItems = await Db.WorkItems.WhereGoal(id.Value).ToArrayAsync();
            }
            else
            {
                int companyId = UserCache.SelectedCompanyId;
                workItems = await Db.WorkItems.WhereCompany(companyId).ToArrayAsync();
            }

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

            workItem.LoadComments(this, "CreateTask");

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

        // POST: WorkItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Create")]
        [Route("Create/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(int? id, [Bind(Include = "AssigneeId,DueDateString,Title,Description,GoalId")] WorkItemViewModel workItemViewModel)
        {
            if (ModelState.IsValid)
            {
                WorkItem workItem = Db.WorkItems.Create();

                Member currentMember = await GetCurrentMemberAsync();

                workItem.CreatedDate = DateTime.Now;
                workItem.CompanyId = currentMember.CompanyId;
                workItem.CreatorId = currentMember.UserId;

                workItem.Title = workItemViewModel.Title;
                workItem.Description = workItemViewModel.Description;
                workItem.DueDate = workItemViewModel.ParsedDueDateTime();
                workItem.AssigneeId = workItemViewModel.AssigneeId;

                if (id.HasValue)
                {
                    // TODO: Validate this goal ID is appropo to the user
                    Goal goal = await Db.Goals.FindAsync(id.Value);
                    workItem.Goal = goal;

                    goal.WorkItems.Add(workItem);
                }

                Db.WorkItems.Add(workItem);
                await Db.SaveChangesAsync();

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
        public async Task<ActionResult> Edit([Bind(Include = "Id,AssigneeId,DueDateString,Title,Description")] WorkItemViewModel workItemViewModel)
        {
            if (ModelState.IsValid)
            {
                WorkItem workItem = await Db.WorkItems.FindAsync(workItemViewModel.Id);

                workItem.Title = workItemViewModel.Title;
                workItem.Description = workItemViewModel.Description;
                workItem.DueDate = workItemViewModel.ParsedDueDateTime();

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
            WorkItem workItem = await Db.WorkItems.FindAsync(id);
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
            ApplicationUser user = GetCurrentUser();
            WorkItem[] workItems = Db.WorkItems.WhereAssignedUser(user.Id).ToArray();

            return View(workItems);
        }
    }
}
