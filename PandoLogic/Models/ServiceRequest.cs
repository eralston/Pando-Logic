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
using System.Web.Mvc;

namespace PandoLogic.Models
{
    /// <summary>
    /// A service request is a distinct instance of a user requesting a service
    /// On ModelBase, CreatedDate indicates when the service was requested
    /// </summary>
    public class ServiceRequest : ModelBase
    {
        // To-One on Company (Optional)
        [ForeignKey("Company")]
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // To-One on User (The person who ordered the service)
        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // To-One on Service (The product purchased)
        [ForeignKey("Service")]
        public int ServiceId { get; set; }
        public virtual Service Service { get; set; }

        /// <summary>
        /// A field for compressing all Service-related 
        /// </summary>
        public string OrderDetails { get; set; }

        /// <summary>
        /// Gets or sets the ID associated with this transaction in the payment system
        /// </summary>
        public string PaymentSystemId { get; set; }

        /// <summary>
        /// Gets or sets the date this request was fulfilled
        /// </summary>
        public DateTime? FulfillmentDateUtc { get; set; }
    }

    /// <summary>
    /// Extensions related to the ServiceRequest class
    /// </summary>
    public static class ServiceRequestExtensions
    {

    }
}