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

using InviteOnly;
using System.Web.Security;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// The various types of notifications for the user, which will drive the presentation style for the user
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// A class for capturing top-level notifications for the user
    /// </summary>
    public class Notification
    {
        public Notification(string title, string message = "", NotificationType type = NotificationType.Info)
        {
            this.Title = title;
            this.Message = message;
            this.Type = type;
        }

        public Notification(string title, NotificationType type = NotificationType.Info)
        {
            this.Title = title;
            this.Type = type;
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
    }

    public class BaseController : Controller, IInviteContextProvider, IPersistenceContext, PandoLogic.Controllers.IDataProvider
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

            public UserInfoCache(ApplicationUser user, Company[] userCompanies, Member selectedMember, Goal[] goals, bool isAdmin)
            {
                // Load user data
                FirstName = user.FirstName;
                LastName = user.LastName;
                Email = user.Email;
                Id = user.Id;
                if (user.Avatar != null)
                    AvatarUrl = user.Avatar.Url;
                else
                    AvatarUrl = null;

                IsAdmin = isAdmin;

                this.Goals = goals;

                if (selectedMember != null)
                {
                    // Load selected member data
                    SelectedCompanyId = selectedMember.CompanyId;
                    JobTitle = selectedMember.JobTitle;
                    SelectedCompanyName = selectedMember.Company.Name;
                    if (selectedMember.Company.Avatar != null)
                        SelectedCompanyAvatarUrl = selectedMember.Company.Avatar.Url;
                    else
                        SelectedCompanyAvatarUrl = null;

                    SelectedMemberId = selectedMember.Id;
                }

                // Load up the companies
                CompanyInfoCache[] companies = new CompanyInfoCache[userCompanies.Length];
                for (int i = 0; i < userCompanies.Length; ++i)
                {
                    Company company = userCompanies[i];
                    companies[i] = new CompanyInfoCache(company);
                }
                Companies = companies;    
            }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string JobTitle { get; set; }
            public string Id { get; set; }
            public string AvatarUrl { get; set; }

            public int SelectedMemberId { get; set; }
            public int SelectedCompanyId { get; set; }
            public string SelectedCompanyName { get; set; }
            public string SelectedCompanyAvatarUrl { get; set; }
            public CompanyInfoCache[] Companies { get; set; }

            public bool IsAdmin { get; set; }

            public Goal[] Goals { get; set; }
        }

        #endregion

        #region Fields

        private ApplicationUser _user = null;
        private Member _member = null;

        #endregion

        #region Properties

        List<Notification> _notifications = null;
        public List<Notification> Notifications
        {
            get
            {
                _notifications = _notifications ?? new List<Notification>();
                return _notifications;
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

        private ActivityRepository _repo = null;
        public async Task<ActivityRepository> GetActivityRepositoryForCurrentCompany()
        {
            if(_repo == null)
            {
                Member currentMember = await GetCurrentMemberAsync();
                if (currentMember == null)
                    return null;

                _repo = ActivityRepository.CreateForCompany(currentMember.CompanyId);
            }

            return _repo;
        }

        private string _username = null;
        /// <summary>
        /// Gets the currently logged in user's name
        /// </summary>
        public string CurrentUsername
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

                if (_userCache != null)
                    return _userCache;

                _userCache = HttpContext.Session[UserInfoCacheId] as UserInfoCache;

                if (_userCache == null)
                {
                    UpdateCurrentUserCache();
                }

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

        protected void ClearCurrentUserCache()
        {
            _userCache = null;
            HttpContext.Session[UserInfoCacheId] = null;
        }

        protected async Task UpdateCurrentUserCacheGoalsAsync()
        {
            int selectedId = UserCache.SelectedCompanyId;
            UserCache.Goals = await Db.Goals.WhereActiveGoalForCompany(selectedId).ToArrayAsync();
        }

        protected void UpdateCurrentUserCache()
        {
            _member = null;
            ClearCurrentUserCache();

            ApplicationUser user = GetCurrentUser();
            if (user == null)
            {
                System.Diagnostics.Trace.TraceError("Trying to update user cache for non-existent user.");
                return;
            }
                
            Company[] userCompanies = Db.CompaniesWhereUserIsMember(user.Id).ToArray();
            Member selectedMember = Db.Members.FindSelectedForUser(user.Id).FirstOrDefault();

            Goal[] goals = null;
            if(selectedMember != null)
            {
                goals = Db.Goals.WhereActiveGoalForCompany(selectedMember.CompanyId).ToArray();
            }
            else
            {
                goals = new Goal[] { };
            }

            Adjudicator adjudicator = new Adjudicator(Db);
            bool isAdmin = adjudicator.IsInAdminRole(user);

            _userCache = new UserInfoCache(user, userCompanies, selectedMember, goals, isAdmin);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        protected async Task UpdateCurrentUserCacheAsync()
        {
            _member = null;
            ClearCurrentUserCache();

            ApplicationUser user = await GetCurrentUserAsync();

            if(user == null)
            {
                System.Diagnostics.Trace.TraceError("Trying to async update user cache for non-existent user.");
                return;
            }

            Company[] companies = await Db.CompaniesWhereUserIsMember(user.Id).ToArrayAsync();
            Member selectedMember = await GetCurrentMemberAsync();

            Goal[] goals;
            if(selectedMember != null)
            {
                goals = await Db.Goals.WhereActiveGoalForCompany(selectedMember.CompanyId).ToArrayAsync();
            }
            else
            {
                goals = new Goal[] { };
            }
            
            _member = selectedMember;

            Adjudicator adjudicator = new Adjudicator(Db);
            bool isAdmin = adjudicator.IsInAdminRole(user);

            _userCache = new UserInfoCache(user, companies, selectedMember, goals, isAdmin);
            HttpContext.Session[UserInfoCacheId] = _userCache;
        }

        public ApplicationUser GetCurrentUser()
        {
            _user = _user ?? this.FindCurrentUser();

            return _user;
        }

        public async Task<ApplicationUser> GetCurrentUserAsync()
        {
            _user = _user ?? await this.FindCurrentUserAsync();

            return _user;
        }

        protected Member GetCurrentMember()
        {
            if (_member != null)
                return _member;

            if(_userCache != null)
            {
                int selectedMemberId = UserCache.SelectedMemberId;
                _member = Db.Members.Find(selectedMemberId);
            }
            else
            {
                ApplicationUser user = GetCurrentUser();
                _member = Db.Members.FindSelectedForUser(user.Id).FirstOrDefault();
            }
           
            return _member;
        }

        public async Task<Member> GetCurrentMemberAsync()
        {
            if (_member != null)
                return _member;
            
            if(_userCache != null)
            {
                int selectedMemberId = UserCache.SelectedMemberId;
                _member = await Db.Members.FindAsync(selectedMemberId);
            }
            else
            {
                ApplicationUser user = await GetCurrentUserAsync();
                _member = await Db.Members.FindSelectedForUser(user.Id).FirstOrDefaultAsync();
            }

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

        /// <summary>
        /// Applies the user cache (lazy loading it the first time) and applies it to the ViewBag
        /// </summary>
        private void SetupUserCacheAndApplyToViewBag()
        {
            if (!Request.IsAuthenticated)
                return;
            
            // Apply cached user info
            UserInfoCache cache = this.UserCache;

            if (cache != null)
            {
                // User properties
                ViewBag.CurrentUserFirstName = cache.FirstName;
                ViewBag.CurrentUserLastName = cache.LastName;
                ViewBag.CurrentUserEmail = cache.Email;
                ViewBag.CurrentUserId = cache.Id;
                ViewBag.CurrentUserAvatarUrl = cache.AvatarUrl;
                ViewBag.CurrentUserIsAdmin = cache.IsAdmin;

                // Select member properties
                ViewBag.CurrentUserJobTitle = cache.JobTitle;
                ViewBag.CurrentUserSelectedCompanyName = cache.SelectedCompanyName;
                ViewBag.CurrentUserSelectedCompanyId = cache.SelectedCompanyId;
                ViewBag.CurrentUserSelectedCompanyAvatarUrl = cache.SelectedCompanyAvatarUrl;

                // Companies
                ViewBag.CurrentUserCompanies = cache.Companies;

                ViewBag.CurrentUserGoals = cache.Goals;
            }
        }

        private void CheckForInvites()
        {
            // If they aren't authenticated, then there's nothing to check
            if (!Request.IsAuthenticated)
                return;

            ApplicationUser user = GetCurrentUser();

            // TODO: Error log
            if (user == null)
                return;


            string email = user.Email;
            IQueryable<MemberInvite> query = Db.MemberInvites.WhereEmail(email).Include(i => i.Company);
            MemberInvite[] invites = query.ToArray();
            if (invites.Length > 0)
            {
                ViewBag.MemberInvites = invites;
            }
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
        /// Called after an action is authorized, but before it is executed
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Do necessary prep for user cache
            SetupUserCacheAndApplyToViewBag();

            // Check for invites
            CheckForInvites();
        }

        #endregion

        #region IInviteContextProvider

        public IInviteContext InviteContext
        {
            get { return Db; }
        }

        #endregion
    }
}