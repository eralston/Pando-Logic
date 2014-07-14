using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;

using PandoLogic;
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

        public class CompanyInfoCache
        {
            public CompanyInfoCache() { }

            public CompanyInfoCache(Company company)
            {
                Name = company.Name;
                Id = company.Id;
            }

            public string Name { get; set; }
            public int Id { get; set; }
        }

        public class UserInfoCache
        {
            public UserInfoCache() { }

            public UserInfoCache(ApplicationUser user, Company[] userCompanies, Member selectedMember)
            {
                // Load user data
                FirstName = user.FirstName;
                LastName = user.LastName;
                Id = user.Id;
                AvatarUrl = user.AvatarUrl;

                // Load selected member data
                SelectedCompanyId = selectedMember.CompanyId;
                JobTitle = selectedMember.JobTitle;
                SelectedCompanyName = selectedMember.Company.Name;
                SelectedCompanyAvatarUrl = selectedMember.Company.AvatarUrl;
                SelectedMemberId = selectedMember.Id;

                // Load up the companies
                CompanyInfoCache[] companies = new CompanyInfoCache[userCompanies.Length];
                for(int i = 0; i < userCompanies.Length; ++i)
                {
                    Company company = userCompanies[i];
                    companies[i] = new CompanyInfoCache(company);
                }
                Companies = companies;
            }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string JobTitle { get; set; }
            public string Id { get; set; }
            public string AvatarUrl { get; set; }

            public int SelectedMemberId { get; set; }
            public int SelectedCompanyId { get; set; }
            public string SelectedCompanyName { get; set; }
            public string SelectedCompanyAvatarUrl { get; set; }
            public CompanyInfoCache[] Companies { get; set; }
        }
        
        #endregion

        #region Fields

        private ApplicationDbContext _db = null;
        private ApplicationUser _user = null;
        private Company _company = null;
        private Member _member = null;

        #endregion

        #region Properties

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

        PandoStorageManager _storageManager = null;
        public PandoStorageManager StorageManager
        {
            get
            {
                _storageManager = _storageManager ?? new PandoStorageManager();

                return _storageManager;
            }
        }

        #endregion

        #region Methods

        protected void UpdateCurrentUserCache()
        {
            ApplicationUser user = GetCurrentUser();
            Company[] userCompanies = Db.CompaniesWhereUserIsMember(user).ToArray();
            Member selectedMember = Db.Members.FindSelectedForUser(user).FirstOrDefault();
            _userCache = new UserInfoCache(user, userCompanies,selectedMember);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        protected async Task UpdateCurrentUserCacheAsync()
        {
            ApplicationUser user = await GetCurrentUserAsync();

            // TODO: Make these requests parallel
            Company[] companies = await Db.CompaniesWhereUserIsMember(user).ToArrayAsync();
            Member member = await GetCurrentMemberAsync();

            _userCache = new UserInfoCache(user, companies, member);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        protected ApplicationUser GetCurrentUser()
        {
            _user = _user ?? Db.Users.Where(u => u.UserName == CurrentUsername).FirstOrDefault();

            return _user;
        }

        protected async Task<ApplicationUser> GetCurrentUserAsync()
        {
            _user = _user ?? await Db.Users.Where(u => u.UserName == CurrentUsername).FirstOrDefaultAsync();

            return _user;
        }

        protected Member GetCurrentMember()
        {
            if (_member != null)
                return _member;

            ApplicationUser user = GetCurrentUser();

            int selectedMemberId = UserCache.SelectedMemberId;
            _member = Db.Members.Find(selectedMemberId);
            return _member;
        }

        protected async Task<Member> GetCurrentMemberAsync()
        {
            if(_member != null)
                return _member;

            int selectedMemberId = UserCache.SelectedMemberId;
            _member = await Db.Members.FindAsync(selectedMemberId);

            return _member;
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
                // User properties
                ViewBag.CurrentUserFirstName = cache.FirstName;
                ViewBag.CurrentUserLastName = cache.LastName;
                ViewBag.CurrentUserId = cache.Id;
                ViewBag.CurrentUserAvatarUrl = cache.AvatarUrl;

                // Select member properties
                ViewBag.CurrentUserJobTitle = cache.JobTitle;
                ViewBag.CurrentUserSelectedCompanyName = cache.SelectedCompanyName;
                ViewBag.CurrentUserSelectedCompanyId = cache.SelectedCompanyId;
                ViewBag.CurrentUserSelectedCompanyAvatarUrl = cache.SelectedCompanyAvatarUrl;

                // Companies
                ViewBag.CurrentUserCompanies = cache.Companies;                
            }            
        }

        #endregion

    }
}