﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Enumerates the various types of phone numbers that people or companies may have in the system
    /// </summary>
    public enum PhoneNumberType
    {
        Mobile,
        Office,
        Home,
        Other
    }

    /// <summary>
    /// Model for tracking a phone number (for an individual or company)
    /// </summary>
    public class PhoneNumber : ModelBase
    {
        [Display(Name = "Phone Number")]
        [Required]
        public string Number { get; set; }

        public string Extension { get; set; }

        public PhoneNumberType Type { get; set; }
    }
}