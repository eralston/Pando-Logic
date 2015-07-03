using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

using PandoLogic.Models;
using Microsoft.AspNet.Identity;

namespace PandoLogic.Migrations
{
    /// <summary>
    /// Performs configuration and seeing of the database
    /// NOTE: In the future this should use explicit migrations, right now it uses automatic migrations
    /// </summary>
    internal sealed class Configuration : DbMigrationsConfiguration<PandoLogic.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        /// <summary>
        /// Creates the initial identify for the given e-mail and password, adding them to the admin group
        /// </summary>
        /// <returns></returns>
        private static void AddToAdminRole(ApplicationDbContext context, string email)
        {
            ApplicationUser user = context.Users.FindByEmail(email);
            if (user == null)
            {
                return;
            }
            else
            {
                Adjudicator adjudicator = new Adjudicator(context);

                adjudicator.AddToAdminRole(user);

                context.SaveChanges();
            }            
        }

        private static void CreateOrUpdateIndustries(PandoLogic.Models.ApplicationDbContext context)
        {
            context.Industries.AddOrUpdate(
                    i => i.Title,
                    new Industry { Title = "Agriculture", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Arts", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Consumer Goods", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Construction", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Corporate Services", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Education", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Finance", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Government", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "High Tech", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Legal", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Manufacturing", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Media", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Medical and Health Care", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Organizations & Non-Profits", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Recreation, Travel, and Entertainment", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Service", CreatedDateUtc = DateTime.UtcNow },
                    new Industry { Title = "Transportation", CreatedDateUtc = DateTime.UtcNow }
                );
        }

        protected override void Seed(PandoLogic.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            AddToAdminRole(context, "erik.ralston@gmail.com");
            AddToAdminRole(context, "erik@bizsprout.net");
            AddToAdminRole(context, "simon@bizsprout.net");

            CreateOrUpdateIndustries(context);
        }

        
    }
}
