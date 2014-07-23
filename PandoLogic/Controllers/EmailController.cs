using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// Controller
    /// </summary>
    public class EmailController : Controller
    {
        // GET: Email
        public ActionResult Generic()
        {
            ViewBag.Title = "Welcome to Pando Logic";
            ViewBag.Teaser = "Short Teaser";
            ViewBag.H1 = "Header Text";
            ViewBag.Lead = "Lead Test Over Here";
            ViewBag.Body = "Body Text Down Here";
            ViewBag.Link = "http://google.com";
            ViewBag.LinkText = "Click to Verify E-Mail Address";

            return View();
        }
    }
}