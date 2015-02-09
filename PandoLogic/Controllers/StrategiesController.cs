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
using System.Web.Routing;

namespace PandoLogic.Controllers
{
    [NotMapped]
    public class ParentWorkItemViewModel
    {
        #region Properties

        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public virtual string Title { get; set; }

        [ConditionalRequired(IgnoreFlagName = "IsSummaryRequired", IgnoreFlagValue = false)]
        [MaxLength(200)]
        public string Summary { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public StrategyInterval Interval { get; set; }

        public bool IsSummaryRequired { get; set; }

        public List<ChildWorkItemViewModel> Children { get; set; }

        #endregion

        #region Methods

        public ParentWorkItemViewModel() { }

        public ParentWorkItemViewModel(Strategy strategy)
        {
            Id = strategy.Id;
            Title = strategy.Title;
            Summary = strategy.Summary;
            Description = strategy.Description;
            Children = new List<ChildWorkItemViewModel>();
        }

        public ParentWorkItemViewModel(Goal goal)
        {
            Id = goal.Id;
            Title = goal.Title;
            Description = goal.Description;
            Children = new List<ChildWorkItemViewModel>();
        }

        public void CreateChildren(int count = 1)
        {
            if (Children == null)
            {
                Children = new List<ChildWorkItemViewModel>();
            }
            for (int i = 0; i < count; i++)
            {
                Children.Add(new ChildWorkItemViewModel());
            }
        }

        public void MarkOrder()
        {
            int i = 1;
            foreach (ChildWorkItemViewModel phase in Children)
            {
                phase.Ordinal = i;
                ++i;
            }
        }

        #endregion
    }

    [NotMapped]
    public class ChildWorkItemViewModel
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
        #region Methods

        private void ValidateHasChildren(ParentWorkItemViewModel strategyViewModel, string errorMessage)
        {
            // Validate children
            if (strategyViewModel.Children != null)
            {
                strategyViewModel.Children = strategyViewModel.Children.Where(c => c.IsMarkedForDelete == false).ToList();
            }

            if (strategyViewModel.Children == null || strategyViewModel.Children.Count == 0)
            {
                ModelState.AddModelError("Custom", errorMessage);
            }
        }

        #endregion

        // GET: Strategies
        public async Task<ActionResult> Index()
        {
            var strategies = await Db.Strategies.WhereLatestFive().ToArrayAsync();
            ViewBag.BookmarkedStrategies = await Db.StrategyBookmarks.StrategiesWhereBookmarkedByUserAsync(UserCache.Id);
            ViewBag.MyStrategies = await Db.Strategies.WhereMadeByUser(UserCache.Id).ToArrayAsync();
            return View(strategies);
        }

        // GET: Strategies/Details/5
        public async Task<ActionResult> Details(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }

            // Apply bookmark state
            ViewBag.IsStrategyBookmarked = await Db.StrategyBookmarks.IsBookmarked(UserCache.Id, id);

            // Apply whether or not this is my strategy
            ViewBag.IsMyStrategy = strategy.AuthorId == UserCache.Id;

            // Setup the rating
            StrategyRating rating = await Db.StrategyRatings.FindForUserAsync(UserCache.Id, id);
            if (rating == null)
            {
                rating = new StrategyRating();
                rating.StrategyId = id;
            }
            ViewBag.StrategyRating = rating;

            // Setup comments
            strategy.LoadComments(this, "CreateStrategy");

            // Setup goals
            strategy.MarkOrder();
            
            return View(strategy);
        }

        // GET: Strategies/Create
        public ActionResult Create()
        {
            ParentWorkItemViewModel viewModel = new ParentWorkItemViewModel();
            viewModel.CreateChildren();
            viewModel.MarkOrder();
            viewModel.IsSummaryRequired = true;
            return View(viewModel);
        }

        // POST: Strategies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ParentWorkItemViewModel strategyViewModel)
        {
            ValidateHasChildren(strategyViewModel, "Strategies must contain at least one goal");

            if (ModelState.IsValid)
            {
                ApplicationUser user = await GetCurrentUserAsync();

                Strategy strategy = Db.Strategies.Create();
                strategy.CreatedDate = DateTime.UtcNow;
                strategy.AuthorId = user.Id;

                strategy.Title = strategyViewModel.Title;
                strategy.Summary = strategyViewModel.Summary;
                strategy.Description = strategyViewModel.Description;
                strategy.Interval = strategyViewModel.Interval;

                strategy.UpdateSearchText();

                Db.Strategies.Add(strategy);

                foreach (ChildWorkItemViewModel strategyGoal in strategyViewModel.Children)
                {
                    Goal goal = new Goal();
                    goal.Title = strategyGoal.Title;
                    goal.Description = strategyGoal.Description;
                    strategy.AddCopyOfGoalAsTemplate(goal);
                }

                await Db.SaveChangesAsync();

                int goalId = strategy.Goals.OrderBy(g => g.Id).First().GoalId;

                return RedirectToAction("CreateTasks", new { id = goalId });
            }

            strategyViewModel.IsSummaryRequired = true;
            strategyViewModel.MarkOrder();
            return View(strategyViewModel);
        }

        public async Task<ActionResult> CreateTasks(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            ParentWorkItemViewModel strategyVm = new ParentWorkItemViewModel(goal);
            strategyVm.CreateChildren(1);
            strategyVm.MarkOrder();
            strategyVm.IsSummaryRequired = false;
            return View(strategyVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTasks(ParentWorkItemViewModel goalViewModel)
        {
            ValidateHasChildren(goalViewModel, "Goals must contain at least one task");

            if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalViewModel.Id);
                ApplicationUser user = await GetCurrentUserAsync();

                foreach (ChildWorkItemViewModel taskViewModel in goalViewModel.Children)
                {
                    WorkItem task = Db.WorkItems.Create();
                    task.Title = taskViewModel.Title;
                    task.Description = taskViewModel.Description;
                    task.IsTemplate = true;
                    task.CreatedDate = DateTime.UtcNow;
                    task.CreatorId = user.Id;
                    goal.WorkItems.Add(task);
                }

                await Db.SaveChangesAsync();

                StrategyGoal strategyGoal = await Db.StrategyGoals.Where(sg => sg.GoalId == goal.Id).Include(sg => sg.Goal).Include(sg => sg.Strategy.Goals).FirstOrDefaultAsync();
                StrategyGoal[] goals = strategyGoal.Strategy.Goals.OrderBy(g => g.Id).ToArray();
                int index = Array.IndexOf(goals, strategyGoal);
                int next = index + 1;
                if (next == goals.Length)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    StrategyGoal sg = goals[next];
                    return RedirectToAction("CreateTasks", new { id = sg.GoalId });
                }
            }

            goalViewModel.IsSummaryRequired = false;
            goalViewModel.MarkOrder();

            return View(goalViewModel);
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
                strategy.UpdateSearchText();
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
            strategy.IsDeleted = true;
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Bookmark(int id)
        {
            StrategyBookmark bookmark = await Db.StrategyBookmarks.FindBookmarkAsync(UserCache.Id, id);
            if (bookmark == null)
            {
                bookmark = Db.StrategyBookmarks.Create(UserCache.Id, id);

                await Db.SaveChangesAsync();
            }


            return RedirectToAction("Details", new { id = id });
        }

        public async Task<ActionResult> Unbookmark(int id)
        {
            StrategyBookmark bookmark = await Db.StrategyBookmarks.FindBookmarkAsync(UserCache.Id, id);
            if (bookmark != null)
            {
                Db.StrategyBookmarks.Remove(bookmark);

                await Db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = id });
        }

        public async Task<ActionResult> Adopt(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }
            return View(strategy);
        }

        // POST: Strategies/Delete/5
        [HttpPost, ActionName("Adopt")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AdoptConfirmed(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);

            strategy.Adopt(Db, UserCache.Id, UserCache.SelectedCompanyId);

            // Setup the new activity and save
            Member member = await GetCurrentMemberAsync();
            Activity newActivity = Db.Activities.Create(member.UserId, member.Company, "");
            newActivity.SetTitle(strategy.Title, Url.Action("Details", "Strategies", new { id = strategy.Id }));
            newActivity.Description = "Strategy Adopted";
            newActivity.Type = ActivityType.WorkAdded;

            await Db.SaveChangesAsync();

            return RedirectToAction("Index", "Goals");
        }

        public async Task<ActionResult> RebuildIndex()
        {
            Strategy[] strategies = await Db.Strategies.ToArrayAsync();

            foreach (Strategy s in strategies)
            {
                s.UpdateSearchText();
            }

            await Db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Exchange(string sort, string search)
        {
            search = search ?? "";
            sort = sort ?? "";

            ViewBag.SearchTerm = search;
            ViewBag.SortId = sort;

            search = search.ToUpper();

            var query = Db.SearchStrategies();

            switch (sort)
            {
                case "popularity":
                    ViewBag.SortOrder = "Popularity";
                    query = query.OrderByDescending(s => s.Adoptions.Count);
                    break;

                case "rating":
                    ViewBag.SortOrder = "Rating";
                    query = query.OrderByDescending(s => s.Rating);
                    break;

                default:
                    ViewBag.SortOrder = "Created Date";
                    query = query.OrderByDescending(s => s.CreatedDate);
                    break;
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.SearchText.Contains(search));
            }

            ViewBag.StrategyTableEmptyText = "No Strategies Found";

            IEnumerable<Strategy> strategies = await query.ToArrayAsync();

            return View(strategies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rate(int? strategyId, float? rating)
        {
            if (strategyId == null || rating == null)
                return new HttpNotFoundResult();

            StrategyRating strategyRating = await Db.StrategyRatings.FindForUserAsync(UserCache.Id, strategyId.Value);
            if(strategyRating == null)
            {
                strategyRating = Db.StrategyRatings.Create(UserCache.Id, strategyId.Value);
            }

            strategyRating.Rating = rating.Value;

            await Db.SaveChangesAsync();
            
            Strategy strategy = await Db.Strategies.FindAsync(strategyId);
            StrategyRating[] ratings = await Db.StrategyRatings.Where(sr => sr.StrategyId == strategyId).ToArrayAsync();
            if(ratings.Length == 0)
            {
                strategy.Rating = 0f;
            }
            else
            {
                float total = 0f;
                foreach (StrategyRating sRating in ratings)
                {
                    total += sRating.Rating;
                }

                float possible = (float)(ratings.Length);

                strategy.Rating = total / possible;
            }
            
            await Db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = strategyId });
        }
    }
}
