using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

using InviteOnly;

namespace PandoLogic.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IInviteContext
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Industry> Industries { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.Company> Companies { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.Address> Addresses { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.Member> Members { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.Activity> Activities { get; set; }

        public System.Data.Entity.DbSet<InviteOnly.Invite> Invites { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.Goal> Goals { get; set; }

        public System.Data.Entity.DbSet<PandoLogic.Models.WorkItem> WorkItems { get; set; }
    }
}