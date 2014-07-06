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
    public class BaseController : Controller
    {
        private ApplicationDbContext _db = null;

        public ApplicationDbContext Db
        {
            get
            {
                if (_db == null)
                {
                    _db = new ApplicationDbContext();
                }

                return _db;
            }
        }

        private string _username = null;
        /// <summary>
        /// Gets the currently logged in user's name
        /// </summary>
        protected string CurrentUsername
        {
            get
            {
                if (_username == null)
                    _username = this.User.Identity.Name;

                return _username;
            }
        }

        protected Task<ApplicationUser> GetCurrentUserAsync()
        {
            return Db.Users.Where(u => u.UserName == CurrentUsername).FirstOrDefaultAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(_db != null) 
                    _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}