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
        // GET: Goals
        public async Task<ActionResult> Index()
        {
            Member currentMember = await GetCurrentMemberAsync();
            int companyId = currentMember.CompanyId;
            var goals = await Db.Goals.Where(g => g.CompanyId == companyId).ToListAsync();
            return View(goals);
        }

        // GET: Goals/Details/5
        public async Task<ActionResult> Details(int? id)
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
        public async Task<ActionResult> Create([Bind(Include = "DueDateString,Title,Description")] GoalViewModel goalViewModel)
        {
            if (ModelState.IsValid)
            {
                Goal goal = Db.Goals.Create();
                Member currentMember = await GetCurrentMemberAsync();
                ApplicationUser user = await GetCurrentUserAsync();

                goal.DueDate = goalViewModel.ParsedDueDateTime();
                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

                goal.CreatedDate = DateTime.Now;
                goal.CompanyId = currentMember.CompanyId;
                goal.CreatorId = user.Id;

                Db.Goals.Add(goal);
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
        public async Task<ActionResult> Edit([Bind(Include = "Id,DueDateString,Title,Description")] GoalViewModel goalViewModel)
        {
            if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalViewModel.Id);

                goal.DueDate = goalViewModel.ParsedDueDateTime();
                goal.Title = goalViewModel.Title;
                goal.Description = goalViewModel.Description;

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
            Goal goals = await Db.Goals.FindAsync(id);
            Db.Goals.Remove(goals);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
