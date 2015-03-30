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

namespace PandoLogic
{
    /// <summary>
    /// Interface indicating an object can provide a valid application DbContext
    /// </summary>
    public interface IPersistenceContext
    {
        string CurrentUsername { get; }

        PandoLogic.Models.ApplicationDbContext Db { get; }
    }

    public static class IPersistenceContextExtensions
    {
        public static ApplicationUser FindCurrentUser(this IPersistenceContext context)
        {
            return context.Db.Users.Where(u => u.UserName == context.CurrentUsername).FirstOrDefault();
        }

        public static Task<ApplicationUser> FindCurrentUserAsync(this IPersistenceContext context)
        {
            return context.Db.Users.Where(u => u.UserName == context.CurrentUsername).FirstOrDefaultAsync();
        }
    }
}
