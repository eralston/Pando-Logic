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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Industry> Industries { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Member> Members { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<Invite> Invites { get; set; }

        public DbSet<Goal> Goals { get; set; }

        public DbSet<WorkItem> WorkItems { get; set; }

        public DbSet<Strategy> Strategies { get; set; }

        public DbSet<StrategyGoal> StrategyGoals { get; set; }

        public DbSet<StrategyBookmark> StrategyBookmarks { get; set; }

        public DbSet<StrategyAdoption> StrategyAdoptions { get; set; }

        public DbSet<StrategyRating> StrategyRatings { get; set; }

        public DbSet<MemberInvite> MemberInvites { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<CloudFile> CloudFiles { get; set; }
    }
}