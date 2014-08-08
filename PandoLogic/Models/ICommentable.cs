﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PandoLogic.Models
{
    /// <summary>
    /// This interface should be implemented by any model object that wishes to accept comments
    /// </summary>
    public interface ICommentable
    {
        int Id { get; set; }
        ICollection<Activity> Comments { get; set; }
    }

    public static class ICommentableExtensions
    {
        public static void LoadComments(this ICommentable commentable, Controller controller, string commentAction)
        {
            if(commentable.Comments != null)
            {
                controller.ViewBag.Comments = commentable.Comments.OrderByDescending(c => c.CreatedDate);
            }
            else
            {
                controller.ViewBag.Comments = new List<Activity>();
            }
            
            controller.ViewBag.CommentId = commentable.Id;
            controller.ViewBag.CommentAction = commentAction;
        }
    }
}