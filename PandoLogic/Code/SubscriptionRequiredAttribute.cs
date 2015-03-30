using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PandoLogic.Code
{
    /// <summary>
    /// Indicates when a controller or method requires a subscription
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SubscriptionRequiredAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Enforces requiring the current company to be valid in order to proceed with the current action
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // This is only for logged in users
            if (!filterContext.HttpContext.Request.IsAuthenticated)
                return;

            // TODO: Enforce subscription
            // TODO: Different subscription levels?
        }
    }
}