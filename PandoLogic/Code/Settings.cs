using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace PandoLogic
{
    /// <summary>
    /// A static class for pulling system settings
    /// </summary>
    public static class Settings
    {
        static string _siteUrl = ConfigurationManager.AppSettings["SiteUrl"];
        public static string SiteUrl
        {
            get
            {
                return _siteUrl;
            }
        }
    }
}