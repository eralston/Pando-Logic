using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// ViewModel for the analytics dashboard
    /// </summary>
    public class AnalyticsViewModel
    {
        // Users
        public int NumberOfUsers { get; private set; }
        public ApplicationUser[] NewUsers { get; private set; }

        // Subscriptions
        public int NumberOfSubscriptions { get; private set; }
        public Subscription[] NewSubscriptions { get; private set; }

        // Companies
        public int NumberOfCompanies { get; private set; }
        public Company[] NewCompanies { get; private set; }

        public AnalyticsViewModel(IDataProvider dataProvider)
        {
            NumberOfUsers = dataProvider.Db.Users.Count();
            NewUsers = dataProvider.Db.Users.OrderByDescending(u => u.CreatedDate).Take(10).ToArray();

            NumberOfSubscriptions = dataProvider.Db.Subscriptions.Where(s => s.IsSoftDeleted == false).Count();
            NewSubscriptions = dataProvider.Db.Subscriptions
                                .Include(s => s.Company).Include(s => s.Plan).Include(s => s.User)
                                .Where(s => s.IsSoftDeleted == false)
                                .OrderByDescending(s => s.CreatedDateUtc).Take(10).ToArray();

            NumberOfCompanies = dataProvider.Db.Companies.Where(m => m.IsSoftDeleted == false).Count();
            NewCompanies = dataProvider.Db.Companies
                                .Include(c => c.Members)
                                .Where(c => c.IsSoftDeleted == false).OrderByDescending(s => s.CreatedDateUtc).Take(10).ToArray();
        }
    }

    /// <summary>
    /// Controller for analytics in the system, for use by administrators only
    /// </summary>
    [AdminAuthorize]
    public class AnalyticsController : BaseController
    {
        // GET: Analytics
        public ActionResult Index()
        {
            AnalyticsViewModel vm = new AnalyticsViewModel(this);
            return View(vm);
        }
    }
}