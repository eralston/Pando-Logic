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
    [RoutePrefix("Comments")]
    public class CommentsController : BaseController
    {
        #region Methods

        private async Task AddComment(Activity comment, ICommentable commentable)
        {
            if (commentable.Comments == null)
            {
                commentable.Comments = new List<Activity>();
            }
            commentable.Comments.Add(comment);

            comment.CreatedDateUtc = DateTime.UtcNow;
            comment.UserId = UserCache.Id;
            comment.CompanyId = UserCache.SelectedCompanyId;
            comment.Type = ActivityType.Comment;

            string title = string.Format("Comment on {0}", commentable.Title);
            comment.SetTitle(title, Url.Action(commentable.CommentActionName, commentable.CommentControllerName, new { id = commentable.Id }));

            Db.Activities.Add(comment);
            await Db.SaveChangesAsync();
        }

        #endregion

        // GET: Comments
        public async Task<ActionResult> Index()
        {
            var comments = Db.Activities.Include(c => c.User);
            return View(await comments.ToListAsync());
        }

        // GET: Comments/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity comment = await Db.Activities.FindAsync(id);
            if (comment == null)
            {
                return HttpNotFound();
            }
            return View(comment);
        }

        // GET: Comments/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Comments/CreateGoal/{id}
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Title,Description")] Activity comment)
        {
            if(string.IsNullOrWhiteSpace(comment.Description))
            {
                ModelState.AddModelError("Nothing!", "Empty Comment, Please Try Again");
            }
            else if (ModelState.IsValid)
            {
                comment.CreatedDateUtc = DateTime.UtcNow;
                comment.UserId = UserCache.Id;
                comment.CompanyId = UserCache.SelectedCompanyId;

                Db.Activities.Add(comment);
                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(comment);
        }

        // POST: Comments/CreateGoal/{id}
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("CreateGoal/{goalId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateGoal(int goalId, [Bind(Include = "Id,Title,Description")] Activity comment)
        {
            if(string.IsNullOrWhiteSpace(comment.Description))
            {
                ModelState.AddModelError("Nothing!", "Empty Comment, Please Try Again");
            }
            else if (ModelState.IsValid)
            {
                Goal goal = await Db.Goals.FindAsync(goalId);

                // TODO: Check goal is allowed for user

                await AddComment(comment, goal);

                return RedirectToAction("Details", "Goals", new { id = goalId });
            }

            StashModelState();
            return RedirectToAction("Details", "Tasks", new { id = goalId });
        }

        // POST: Comments/CreateTask/{taskId}
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("CreateTask/{taskId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTask(int taskId, [Bind(Include = "Id,Title,Description")] Activity comment)
        {
            if(string.IsNullOrWhiteSpace(comment.Description))
            {
                ModelState.AddModelError("Nothing!", "Empty Comment, Please Try Again");
            }
            else if (ModelState.IsValid)
            {
                WorkItem task = await Db.WorkItems.FindAsync(taskId);

                // TODO: Check tasks allowed for user

                await AddComment(comment, task);

                return RedirectToAction("Details", "Tasks", new { id = taskId });
            }

            StashModelState();
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        // POST: Comments/CreateStrategy/{taskId}
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("CreateStrategy/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStrategy(int id, [Bind(Include = "Id,Title,Description")] Activity comment)
        {
            if (ModelState.IsValid)
            {
                Strategy strategy = await Db.Strategies.FindAsync(id);

                // TODO: Check strategy is allowed for user

                await AddComment(comment, strategy);

                return RedirectToAction("Details", "Strategies", new { id = id });
            }

            StashModelState();
            return RedirectToAction("Details", "Strategies", new { id = id });
        }

        // GET: Comments/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity comment = await Db.Activities.FindAsync(id);
            if (comment == null)
            {
                return HttpNotFound();
            }

            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Description")] Activity commentViewModel)
        {
            if (ModelState.IsValid)
            {
                var comment = await Db.Activities.FindAsync(commentViewModel.Id);
                comment.Title = commentViewModel.Title;
                Db.Entry(comment).State = EntityState.Modified;

                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(commentViewModel);
        }

        // GET: Comments/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Activity comment = await Db.Activities.FindAsync(id);
            if (comment == null)
            {
                return HttpNotFound();
            }
            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Activity comment = await Db.Activities.FindAsync(id);
            Db.Activities.Remove(comment);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
