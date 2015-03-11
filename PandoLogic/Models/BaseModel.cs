using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Security;

namespace PandoLogic.Models
{
    /// <summary>
    /// An interface for describing the base model, such that it's easier to generalize 
    /// </summary>
    public interface IBaseModel
    {
        DateTime? CreatedDateUtc { get; set; }
        int Id { get; set; }
    }

    /// <summary>
    /// A base class for providing the fundamental parts of a model, common to all instances no dependent on framework classes
    /// </summary>
    public abstract class BaseModel : IBaseModel
    {
        // Primary Key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Created Date")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedDateUtc { get; set; }
    }
}