using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

using PandoLogic.Models;

namespace PandoLogic.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<PandoLogic.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
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
