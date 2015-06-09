using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// This interface should be implemented by any model object that wishes to accept comments
    /// </summary>
    public interface ICommentable : IModelBase
    {
        int? CompanyId { get; }
        string CommentControllerName { get; }
        string CommentActionName { get; }

        string Title { get; }
    }

    public static class ICommentableExtensions
    {
        /// <summary>
        /// Extension method on a commentale that
        /// </summary>
        /// <typeparam name="ParentType"></typeparam>
        /// <param name="commentable"></param>
        /// <param name="controller"></param>
        /// <param name="commentAction"></param>
        /// <param name="repo"></param>
        /// <returns></returns>
        public static async Task LoadComments<ParentType>(this ParentType commentable, Controller controller, string commentAction, ActivityRepository repo) where ParentType : ICommentable
        {
            controller.ViewBag.Comments = await repo.Retrieve<ParentType>(commentable.Id);

            controller.ViewBag.CommentId = commentable.Id;
            controller.ViewBag.CommentAction = commentAction;
        }
    }
}
