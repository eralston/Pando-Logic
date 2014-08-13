using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web;

using Microsoft.AspNet.SignalR;

using PandoLogic.Models;

namespace PandoLogic.Hubs
{
    
    public class BaseHub : Hub, IPersistenceContext
    {
        /// <summary>
        /// Gets the currently logged in user's name
        /// </summary>
        public string CurrentUsername
        {
            get
            {
                return this.Context.User.Identity.Name;
            }
        }

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing)
            {
                if (_db != null)
                    _db.Dispose();
            }
        }
    }
}