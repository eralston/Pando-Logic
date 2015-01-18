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

            context.Industries.AddOrUpdate(
                    i => i.Title,
                    new Industry { Title = "Agriculture", CreatedDate = DateTime.Now },
                    new Industry { Title = "Arts", CreatedDate = DateTime.Now },
                    new Industry { Title = "Consumer Goods", CreatedDate = DateTime.Now },
                    new Industry { Title = "Construction", CreatedDate = DateTime.Now },
                    new Industry { Title = "Corporate Services", CreatedDate = DateTime.Now },
                    new Industry { Title = "Education", CreatedDate = DateTime.Now },
                    new Industry { Title = "Finance", CreatedDate = DateTime.Now },
                    new Industry { Title = "Government", CreatedDate = DateTime.Now },
                    new Industry { Title = "High Tech", CreatedDate = DateTime.Now },
                    new Industry { Title = "Legal", CreatedDate = DateTime.Now },
                    new Industry { Title = "Manufacturing", CreatedDate = DateTime.Now },
                    new Industry { Title = "Media", CreatedDate = DateTime.Now },
                    new Industry { Title = "Medical and Health Care", CreatedDate = DateTime.Now },
                    new Industry { Title = "Organizations & Non-Profits", CreatedDate = DateTime.Now },
                    new Industry { Title = "Recreation, Travel, and Entertainment", CreatedDate = DateTime.Now },
                    new Industry { Title = "Service", CreatedDate = DateTime.Now },
                    new Industry { Title = "Transportation", CreatedDate = DateTime.Now }
                );
        }
    }
}
