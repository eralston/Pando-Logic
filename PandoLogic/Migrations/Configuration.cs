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
                    new Industry { Title = "Agriculture", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Arts", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Consumer Goods", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Construction", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Corporate Services", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Education", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Finance", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Government", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "High Tech", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Legal", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Manufacturing", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Media", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Medical and Health Care", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Organizations & Non-Profits", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Recreation, Travel, and Entertainment", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Service", CreatedDate = DateTime.UtcNow },
                    new Industry { Title = "Transportation", CreatedDate = DateTime.UtcNow }
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

            CreateOrUpdateIndustries(context);
        }

        
    }
}
