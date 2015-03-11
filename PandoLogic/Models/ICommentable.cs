using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace PandoLogic.Models
{
    /// <summary>
    /// This interface should be implemented by any model object that wishes to accept comments
    /// </summary>
    public interface ICommentable : IBaseModel
    {
        ICollection<Activity> Comments { get; set; }

        string CommentControllerName { get; }
        string CommentActionName { get; }

        string Title { get; }
    }

    public static class ICommentableExtensions
    {
        public static void LoadComments(this ICommentable commentable, Controller controller, string commentAction)
        {
            if(commentable.Comments != null)
            {
                controller.ViewBag.Comments = commentable.Comments.OrderByDescending(c => c.CreatedDateUtc);
            }
            else
            {
                controller.ViewBag.Comments = new List<Activity>();
            }
            
            controller.ViewBag.CommentId = commentable.Id;
            controller.ViewBag.CommentAction = commentAction;
        }

        public static void RemoveComments(this DbSet<Activity> activites, ICommentable commentable)
        {
            activites.RemoveRange(commentable.Comments);
        }
    }
}
