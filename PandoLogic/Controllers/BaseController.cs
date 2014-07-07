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
        #region Constants

        protected const string UserInfoCacheId = "UserInfoCacheId";
        protected const string ModelStateCacheId = "ModelStateCacheId";

        #endregion

        #region Inner Types

        public class UserInfoCache
        {
            public UserInfoCache() { }

            public UserInfoCache(ApplicationUser user)
            {
                FirstName = user.FirstName;
                LastName = user.LastName;
                JobTitle = user.JobTitle;
                Id = user.Id;
            }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string JobTitle { get; set; }
            public string Id { get; set; }
        }
        
        #endregion

        #region Fields

        private ApplicationDbContext _db = null;
        private ApplicationUser _user = null;

        #endregion

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

        UserInfoCache _userCache = null;
        public UserInfoCache UserCache
        {
            get
            {
                if (!Request.IsAuthenticated)
                {
                    return null;
                }

                _userCache = HttpContext.Session[UserInfoCacheId] as UserInfoCache;

                if (_userCache == null)
                    UpdateCurrentUserCache();

                return _userCache;
            }
        }

        #region Methods

        protected void UpdateCurrentUserCache()
        {
            ApplicationUser user = GetCurrentUser();
            _userCache = new UserInfoCache(user);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        protected async Task UpdateCurrentUserCacheAsync()
        {
            ApplicationUser user = await GetCurrentUserAsync();
            _userCache = new UserInfoCache(user);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        protected ApplicationUser GetCurrentUser()
        {
            if (_user != null)
                return _user;

            _user = Db.Users.Where(u => u.UserName == CurrentUsername).FirstOrDefault();

            return _user;
        }

        protected Task<ApplicationUser> GetCurrentUserAsync()
        {
            if (_user != null)
                return Task<ApplicationUser>.FromResult(_user);

            return Db.Users.Where(u => u.UserName == CurrentUsername).FirstOrDefaultAsync();
        }

        protected void StashModelState()
        {
            TempData[ModelStateCacheId] = ModelState;
        }

        protected void UnstashModelState()
        {
            ModelStateDictionary oldModelState = TempData[ModelStateCacheId] as ModelStateDictionary;
            if (oldModelState != null)
            {
                ModelState.Merge(oldModelState);
            }

            TempData[ModelStateCacheId] = null;
        }

        #endregion

        #region Override Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_db != null)
                    _db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Apply cached user info
            UserInfoCache cache = this.UserCache;

            if (cache != null)
            {
                ViewBag.CurrentUserFirstName = cache.FirstName;
                ViewBag.CurrentUserLastName = cache.LastName;
                ViewBag.CurrentUserJobTitle = cache.JobTitle;
                ViewBag.CurrentUserId = cache.Id;
            }            
        }

        #endregion

    }
}