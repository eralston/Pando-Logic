using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// A type of service offered in the system, which is a one-time purchased consulting opportunity
    /// </summary>
    public class Service : StripeEntities.Product
    {
        [Display(Name = "Action Name")]
        public string ActionName { get; set; }
    }

    /// <summary>
    /// Extensions for the Service model class
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Creates a new instance of a product
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static Service Create(this DbSet<Service> services, string title)
        {
            Service newService = services.Create();

            newService.CreatedDateUtc = DateTime.UtcNow;

            newService.Title = title;

            services.Add(newService);

            return newService;
        }

        public static IQueryable<Service> WhereActive(this DbSet<Service> services)
        {
            return services.Where(s => s.State == StripeEntities.Product.ProductState.Available).OrderByDescending(s => s.Price);
        }
    }
}