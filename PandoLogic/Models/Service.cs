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
using System.ComponentModel;
using System.Web.Mvc;

namespace PandoLogic.Models
{
    public enum ServiceCategory
    {
        Public,
        SproutUniversity
    }

    /// <summary>
    /// A type of service offered in the system, which is a one-time purchased consulting opportunity
    /// </summary>
    public class Service : StripeEntities.Product
    {
        [Display(Name = "Action Name")]
        [Editable(true)]
        public string ActionName { get; set; }

        [Editable(true)]
        public ServiceCategory Category { get; set; }

        [Display(Name = "Offer HTML")]
        [Editable(true)]
        [AllowHtml]
        public string OfferHtml { get; set; }
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

        public static IQueryable<Service> WhereActiveAndPublic(this DbSet<Service> services)
        {
            return services.Where(s => s.State == StripeEntities.Product.ProductState.Available && s.Category == ServiceCategory.Public).OrderByDescending(s => s.Price);
        }
    }
}