using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

using PandoLogic.Models;
using System.ComponentModel.DataAnnotations;

namespace PandoLogic.Controllers
{
    [NotMapped]
    public class StrategyViewModel : Strategy
    {
        public StrategyViewModel() { }

        public StrategyViewModel(Strategy strategy)
        {
            strategy.CopyProperties(this);
            StrategyGoals = new List<StrategyGoalViewModel>();
        }

        public List<StrategyGoalViewModel> StrategyGoals { get; set; }

        public void CreatePhases(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                StrategyGoals.Add(new StrategyGoalViewModel());
            }
        }

        public void MarkPhaseOrder()
        {
            int i = 1;
            foreach (StrategyGoalViewModel phase in StrategyGoals)
            {
                phase.Ordinal = i;
                ++i;
            }
        }
    }

    [NotMapped]
    public class StrategyGoalViewModel
    {
        public int Ordinal { get; set; }

        public bool IsMarkedForDelete { get; set; }

        [ConditionalRequired(IgnoreFlagName = "IsMarkedForDelete", IgnoreFlagValue = true)]
        public string Title { get; set; }

        [DataType(DataType.MultilineText)]
        [ConditionalRequired(IgnoreFlagName = "IsMarkedForDelete", IgnoreFlagValue = true)]
        public string Description { get; set; }
    }

    public class StrategiesController : BaseController
    {
        // GET: Strategies
        public async Task<ActionResult> Index()
        {
            var strategies = Db.Strategies.Include(s => s.Author);
            return View(await strategies.ToListAsync());
        }

        // GET: Strategies/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }
            return View(strategy);
        }

        // GET: Strategies/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Strategies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Summary,Title,Description")] Strategy strategy)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await GetCurrentUserAsync();

                strategy.CreatedDate = DateTime.Now;
                strategy.AuthorId = user.Id;
                Db.Strategies.Add(strategy);

                await Db.SaveChangesAsync();

                return RedirectToAction("CreateGoals", new { id = strategy.Id });
            }
            return View(strategy);
        }

        /// <summary>
        /// Creates 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> CreateGoals(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            StrategyViewModel strategyVm = new StrategyViewModel(strategy);
            strategyVm.CreatePhases(1);

            strategyVm.MarkPhaseOrder();
            return View(strategyVm);
        }

        [HttpPost]
        public async Task<ActionResult> CreateGoals(StrategyViewModel strategyViewModel)
        {
            if (ModelState.IsValid)
            {
                Strategy strategy = await Db.Strategies.FindAsync(strategyViewModel.Id);
                strategy.Interval = strategyViewModel.Interval;

                foreach (StrategyGoalViewModel strategyGoal in strategyViewModel.StrategyGoals)
                {
                    if(!strategyGoal.IsMarkedForDelete)
                    {
                        Goal goal = new Goal();
                        goal.Title = strategyGoal.Title;
                        goal.Description = strategyGoal.Description;
                        strategy.AddCopyOfGoalAsTemplate(goal);
                    }
                }

                Db.Entry(strategy).State = EntityState.Modified;

                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            strategyViewModel.MarkPhaseOrder();

            return View(strategyViewModel);
        }

        // GET: Strategies/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }
            return View(strategy);
        }

        // POST: Strategies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Summary,Title,Description")] Strategy strategy)
        {
            if (ModelState.IsValid)
            {
                Db.Entry(strategy).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(strategy);
        }

        // GET: Strategies/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }
            return View(strategy);
        }

        // POST: Strategies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            Db.Strategies.Remove(strategy);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
