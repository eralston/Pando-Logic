using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// A type for companies, representing a dozen or so possibilities from the database from an owner to choose
    /// </summary>
    public class Industry : ModelBase
    {
        // Required Field
        [Required]
        public string Title { get; set; }
    }
}

