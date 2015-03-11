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
    /// <summary>
    /// Controller for CRUD + Bookmarking and rating of Strategy models
    /// </summary>
    public class StrategiesController : BaseController
    {
        #region Methods

        /// <summary>
        /// Validates that the viewModel has children; otherwise, it adds the error message to the ModelState for this request
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="errorMessage"></param>
        private void ValidateHasChildren(ParentWorkItemViewModel viewModel, string errorMessage)
        {
            // Validate children
            if (viewModel.Children != null)
            {
                viewModel.Children = viewModel.Children.Where(c => c.IsMarkedForDelete == false).ToList();
            }

            if (viewModel.Children == null || viewModel.Children.Count == 0)
            {
                ModelState.AddModelError("Custom", errorMessage);
            }
        }

        /// <summary>
        /// Edits all tasks under a given goal, using the data from the given view model
        /// </summary>
        /// <param name="goalViewModel"></param>
        /// <param name="goal"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task EditWorkItemsUnderGoal(ParentWorkItemViewModel goalViewModel, Goal goal, ApplicationUser user)
        {
            foreach (ChildWorkItemViewModel workItemViewModel in goalViewModel.Children)
            {
                if (workItemViewModel.Id.HasValue)
                {
                    WorkItem workItem = await Db.WorkItems.FindAsync(workItemViewModel.Id);
                    if (workItemViewModel.IsMarkedForDelete)
                    {
                        // If we're nuking this task
                        Db.Activities.RemoveComments(workItem);
                        Db.WorkItems.Remove(workItem);
                    }
                    else
                    {
                        // Edit existing strategy goal
                        workItem.Title = workItemViewModel.Title;
                        workItem.Description = workItemViewModel.Description;
                    }
                }
                else
                {
                    // Skip any already marked for delete
                    if (workItemViewModel.IsMarkedForDelete)
                        continue;

                    // Create bran new work item
                    WorkItem task = Db.WorkItems.Create();
                    task.Title = workItemViewModel.Title;
                    task.Description = workItemViewModel.Description;
                    task.IsTemplate = true;
                    task.CreatedDateUtc = DateTime.UtcNow;
                    task.UserId = user.Id;
                    goal.WorkItems.Add(task);
                }

            }
        }

        /// <summary>
        /// Edits all of the goals under the given strategy using child work item data from the given viewmodel
        /// </summary>
        /// <param name="strategyViewModel"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        private async Task EditGoalsUnderStrategy(ParentWorkItemViewModel strategyViewModel, Strategy strategy, string userId, int companyId)
        {
            foreach (ChildWorkItemViewModel goalViewModel in strategyViewModel.Children)
            {
                if (goalViewModel.RelationshipId.HasValue)
                {
                    StrategyGoal sGoal = await Db.StrategyGoals.FindAsync(goalViewModel.RelationshipId);
                    if (goalViewModel.IsMarkedForDelete)
                    {
                        // If we're nuking this goal
                        Db.Activities.RemoveComments(sGoal.Goal);
                        Db.Goals.Remove(sGoal.Goal);
                        Db.StrategyGoals.Remove(sGoal);
                    }
                    else
                    {
                        // Edit existing strategy goal
                        sGoal.Goal.Title = goalViewModel.Title;
                        sGoal.Goal.Description = goalViewModel.Description;
                    }
                }
                else
                {
                    // Skip any already marked for delete
                    if (goalViewModel.IsMarkedForDelete)
                        continue;

                    // If it's a brand new goal, then create a whole new one!
                    Goal goal = Db.Goals.Create(companyId, userId);
                    strategy.AddCopyOfGoalAsTemplate(goal);
                }
            }
        }

        /// <summary>
        /// Returns a redirect action to either the next goal in the the parent strategy for the parent goal 
        /// OR 
        /// Return a redirect action to the details of the parent strategy if that was the last goal for the parent strategy
        /// </summary>
        /// <param name="goal"></param>
        /// <returns></returns>
        private async Task<ActionResult> RedirectToNextGoalOrStrategyDetails(Goal goal)
        {
            StrategyGoal strategyGoal = await Db.StrategyGoals.Where(sg => sg.GoalId == goal.Id).Include(sg => sg.Goal).Include(sg => sg.Strategy.Goals).FirstOrDefaultAsync();
            StrategyGoal[] goals = strategyGoal.Strategy.Goals.OrderBy(g => g.Id).ToArray();
            int index = Array.IndexOf(goals, strategyGoal);
            int next = index + 1;
            if (next == goals.Length)
            {
                return RedirectToAction("Details", new { id = strategyGoal.StrategyId });
            }
            else
            {
                StrategyGoal sg = goals[next];
                return RedirectToAction("CreateTasks", new { id = sg.GoalId });
            }
        }

        #endregion

        /// <summary>
        /// Gets the strategy dashboard for the current user, including strategies they created and a preview of the exchange
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Index()
        {
            var strategies = await Db.Strategies.WhereLatestFive().ToArrayAsync();
            ViewBag.BookmarkedStrategies = await Db.StrategyBookmarks.StrategiesWhereBookmarkedByUserAsync(UserCache.Id);
            ViewBag.MyStrategies = await Db.Strategies.WhereMadeByUser(UserCache.Id).ToArrayAsync();
            return View(strategies);
        }

        /// <summary>
        /// Gets the details pages for the strategy with the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
            ViewBag.IsMyStrategy = strategy.IsOwnedByUser(UserCache.Id);

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

        /// <summary>
        /// Gets the create page for strategies
        /// </summary>
        /// <returns></returns>
        public ActionResult Create()
        {
            ParentWorkItemViewModel viewModel = new ParentWorkItemViewModel();
            viewModel.CreateChildren();
            viewModel.MarkOrder();
            viewModel.IsSummaryRequired = true;
            return View(viewModel);
        }

        /// <summary>
        /// Posts changes to create a strategy with the given view model for the current user
        /// </summary>
        /// <param name="strategyViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ParentWorkItemViewModel strategyViewModel)
        {
            ValidateHasChildren(strategyViewModel, "Strategies must contain at least one goal");

            if (ModelState.IsValid)
            {
                Member member = await GetCurrentMemberAsync();

                Strategy strategy = Db.Strategies.Create();
                strategy.CreatedDateUtc = DateTime.UtcNow;
                strategy.UserId = UserCache.Id;

                strategy.Title = strategyViewModel.Title;
                strategy.Summary = strategyViewModel.Summary;
                strategy.Description = strategyViewModel.Description;
                strategy.Interval = strategyViewModel.Interval;

                strategy.UpdateSearchText();

                Db.Strategies.Add(strategy);

                await EditGoalsUnderStrategy(strategyViewModel, strategy, UserCache.Id, member.CompanyId);

                await Db.SaveChangesAsync();

                int goalId = strategy.Goals.OrderBy(g => g.Id).First().GoalId;

                return RedirectToAction("CreateTasks", new { id = goalId });
            }

            strategyViewModel.IsSummaryRequired = true;
            strategyViewModel.MarkOrder();
            return View(strategyViewModel);
        }

        /// <summary>
        /// Gets the page for creating workitems under the givne goal ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> CreateTasks(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);
            ParentWorkItemViewModel strategyVm = new ParentWorkItemViewModel(goal);
            strategyVm.CreateChildren(1);
            strategyVm.MarkOrder();
            strategyVm.IsSummaryRequired = false;
            return View(strategyVm);
        }

        /// <summary>
        /// Posts create info to the given goal's work item children
        /// </summary>
        /// <param name="goalViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTasks(ParentWorkItemViewModel goalViewModel)
        {
            ValidateHasChildren(goalViewModel, "Goals must contain at least one task");

            if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalViewModel.Id);
                ApplicationUser user = await GetCurrentUserAsync();

                await EditWorkItemsUnderGoal(goalViewModel, goal, user);

                await Db.SaveChangesAsync();

                return await RedirectToNextGoalOrStrategyDetails(goal);
            }

            goalViewModel.IsSummaryRequired = false;
            goalViewModel.MarkOrder();

            return View(goalViewModel);
        }

        /// <summary>
        /// Gets the edit page for changing the given strategy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Edit(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);

            if (strategy == null)
            {
                return HttpNotFound();
            }

            strategy.ExceptIfNotOwnedByUser(this.UserCache.Id);

            ParentWorkItemViewModel viewModel = new ParentWorkItemViewModel(strategy);

            return View(viewModel);
        }

        // POST: Strategies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ParentWorkItemViewModel strategyViewModel)
        {
            ValidateHasChildren(strategyViewModel, "Strategies must contain at least one goal");

            if (ModelState.IsValid)
            {
                Member member = await GetCurrentMemberAsync();

                Strategy strategy = await Db.Strategies.FindAsync(strategyViewModel.Id);

                strategy.ExceptIfNotOwnedByUser(this.UserCache.Id);

                strategy.UserId = UserCache.Id;
                strategy.Title = strategyViewModel.Title;
                strategy.Summary = strategyViewModel.Summary;
                strategy.Description = strategyViewModel.Description;
                strategy.Interval = strategyViewModel.Interval;

                strategy.UpdateSearchText();

                await EditGoalsUnderStrategy(strategyViewModel, strategy, UserCache.Id, member.CompanyId);

                await Db.SaveChangesAsync();

                // Redirect to the first goal under this strategy
                int goalId = strategy.Goals.OrderBy(g => g.Id).First().GoalId;

                return RedirectToAction("EditTasks", new { id = goalId });
            }

            strategyViewModel.IsSummaryRequired = true;
            strategyViewModel.MarkOrder();
            
            return View(strategyViewModel);
        }

        /// <summary>
        /// Edits the tasks with goal matching the given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> EditTasks(int id)
        {
            Goal goal = await Db.Goals.FindAsync(id);

            // TODO: Verify rights to modify this goal

            ParentWorkItemViewModel strategyVm = new ParentWorkItemViewModel(goal);
            strategyVm.CreateChildren(1);
            strategyVm.MarkOrder();
            strategyVm.IsSummaryRequired = false;
            return View(strategyVm);
        }

        /// <summary>
        /// Posts the changes to edit the tasks under a given goal view model
        /// </summary>
        /// <param name="goalViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditTasks(ParentWorkItemViewModel goalViewModel)
        {
            ValidateHasChildren(goalViewModel, "Goals must contain at least one task");

            if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalViewModel.Id);
                ApplicationUser user = await GetCurrentUserAsync();

                await EditWorkItemsUnderGoal(goalViewModel, goal, user);

                await Db.SaveChangesAsync();

                return await RedirectToNextGoalOrStrategyDetails(goal);
            }

            goalViewModel.IsSummaryRequired = false;
            goalViewModel.MarkOrder();

            return View(goalViewModel);
        }

        /// <summary>
        /// Gets the delete confirmation page for the given goal
        /// NOTE: Users should only be able to fulfill this action if they "own" the strategy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Delete(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }

            strategy.ExceptIfNotOwnedByUser(UserCache.Id);

            return View(strategy);
        }

        /// <summary>
        /// Posts the confirmation to delete the given strategy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            
            strategy.ExceptIfNotOwnedByUser(UserCache.Id);

            // Mark as deleted
            strategy.IsDeleted = true;
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Bookmarks a strategy with the given ID for the current user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Bookmark(int id)
        {
            StrategyBookmark bookmark = await Db.StrategyBookmarks.FindBookmarkByUserAndStrategyAsync(UserCache.Id, id);
            if (bookmark == null)
            {
                bookmark = Db.StrategyBookmarks.Create(UserCache.Id, id);

                await Db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = id });
        }

        /// <summary>
        /// Deletes the bookmark for a strategy with the given ID for the current user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Unbookmark(int id)
        {
            StrategyBookmark bookmark = await Db.StrategyBookmarks.FindBookmarkByUserAndStrategyAsync(UserCache.Id, id);
            if (bookmark != null)
            {
                Db.StrategyBookmarks.Remove(bookmark);

                await Db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = id });
        }

        /// <summary>
        /// Gets the confirmation screen for the user to adopt a given strategy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Adopt(int id)
        {
            Strategy strategy = await Db.Strategies.FindAsync(id);
            if (strategy == null)
            {
                return HttpNotFound();
            }
            return View(strategy);
        }

        /// <summary>
        /// Posts to cause a user to adopt a strategy, replicating its goals and tasks into the current user's system
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

            await UpdateCurrentUserCacheGoalsAsync();

            return RedirectToAction("Index", "Goals");
        }

        /// <summary>
        /// Rebuilds the search field for ALL strategies in the system
        /// WARNING: This might take a while in a production system and should only be used for holy shit debug purposes
        /// TODO: Convert search to Azure search-as-a-service
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the exchange page, which is a list of all strategies available for public review in the 
        /// </summary>
        /// <param name="sort"></param>
        /// <param name="search"></param>
        /// <returns></returns>
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
                    query = query.OrderByDescending(s => s.CreatedDateUtc);
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

        /// <summary>
        /// Adds another rating to the database for the given strategy and rating
        /// This will re-calculate ratings for the given strategy based on the current values in the database
        /// </summary>
        /// <param name="strategyId"></param>
        /// <param name="rating"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Rate(int? strategyId, float? rating)
        {
            if (strategyId == null || rating == null)
                return new HttpNotFoundResult();

            StrategyRating strategyRating = await Db.StrategyRatings.FindForUserAsync(UserCache.Id, strategyId.Value);
            if (strategyRating == null)
            {
                strategyRating = Db.StrategyRatings.Create(UserCache.Id, strategyId.Value);
            }

            strategyRating.Rating = rating.Value;

            await Db.SaveChangesAsync();

            // TODO: Make this much more efficient by making the database do it with views when querying
            Strategy strategy = await Db.Strategies.FindAsync(strategyId);
            StrategyRating[] ratings = await Db.StrategyRatings.Where(sr => sr.StrategyId == strategyId).ToArrayAsync();
            if (ratings.Length == 0)
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
