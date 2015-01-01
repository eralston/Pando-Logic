using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

using PandoLogic.Models;

namespace PandoLogic
{
    /// <summary>
    /// A class for extension methods concerning roles
    /// </summary>
    public static class RoleExtensions
    {
        public static void EnsureRoleExists(this RoleManager<IdentityRole> roleManager, string roleName)
        {
            // Check to see if Role Exists, if not create it
            if (!roleManager.RoleExists(roleName))
                roleManager.Create(new IdentityRole(roleName));
        }
    }

    /// <summary>
    /// Simplifies using the UserManager & RoleManager classes
    /// </summary>
    public class Adjudicator : IDisposable
    {
        public const string AdminRoleId = "admin";

        ApplicationDbContext _dbContext;
        public Adjudicator(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private RoleManager<IdentityRole> _roleManager = null;
        public RoleManager<IdentityRole> RoleManager
        {
            get
            {
                if (_roleManager == null)
                    _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(_dbContext));

                return _roleManager;
            }
        }

        private UserManager<ApplicationUser> _userManager = null;
        public UserManager<ApplicationUser> UserManager
        {
            get
            {
                if (_userManager == null)
                    _userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(_dbContext));

                return _userManager;
            }
        }

        public bool IsInAdminRole(ApplicationUser user)
        {
            return UserManager.IsInRole(user.Id, AdminRoleId);
        }

        public void AddToAdminRole(ApplicationUser user)
        {
            RoleManager.EnsureRoleExists(AdminRoleId);
            UserManager.AddToRole(user.Id, AdminRoleId);
        }

        public void RemoveFromAdminRole(ApplicationUser user)
        {
            RoleManager.EnsureRoleExists(AdminRoleId);
            UserManager.RemoveFromRole(user.Id, AdminRoleId);
        }

        public void Dispose()
        {
            if (_userManager != null)
                _userManager.Dispose();

            if (_roleManager != null)
                _roleManager.Dispose();
        }
    }
}