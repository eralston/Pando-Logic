using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PandoLogic
{
    /// <summary>
    /// Apply to a controller or action method to prevent non-admin users from accessing it
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        public AdminAuthorizeAttribute()
        {
            this.Roles = Adjudicator.AdminRoleId;
        }
    }
}