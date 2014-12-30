﻿using System;
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

    public class BaseController : Controller, IInviteContextProvider, IPersistenceContext
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

                if (selectedMember != null)
                {
                    // Load selected member data
                    SelectedCompanyId = selectedMember.CompanyId;
                    JobTitle = selectedMember.JobTitle;
                    SelectedCompanyName = selectedMember.Company.Name;
                    SelectedCompanyAvatarUrl = selectedMember.Company.AvatarUrl;
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

        protected void ClearCurrentUserCache()
        {
            HttpContext.Session[UserInfoCacheId] = null;
        }

        protected void UpdateCurrentUserCache()
        {
            ApplicationUser user = GetCurrentUser();
            Company[] userCompanies = Db.CompaniesWhereUserIsMember(user).ToArray();
            Member selectedMember = Db.Members.FindSelectedForUser(user).FirstOrDefault();
            _userCache = new UserInfoCache(user, userCompanies, selectedMember);
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

            int selectedMemberId = UserCache.SelectedMemberId;
            _member = Db.Members.Find(selectedMemberId);
            return _member;
        }

        protected async Task<Member> GetCurrentMemberAsync()
        {
            if (_member != null)
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

        /// <summary>
        /// Applies the user cache (lazy loading it the first time) and applies it to the ViewBag
        /// </summary>
        private void SetupUserCacheAndApplyToViewBag()
        {
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

                ViewBag.CurrentUserGoals = Db.Goals.Where(g => g.ArchiveDate == null && g.IsTemplate == false && g.CompanyId == cache.SelectedCompanyId).Include(g => g.WorkItems).OrderBy(g => g.DueDate).ToArray();
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

        private void CheckForInvites()
        {
            ApplicationUser user = GetCurrentUser();
            string email = user.Email;
            IQueryable<MemberInvite> query = Db.MemberInvites.WhereEmail(email).Include(i => i.Company);
            MemberInvite[] invites = query.ToArray();
            if (invites.Length > 0)
            {
                ViewBag.MemberInvites = invites;
            }
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