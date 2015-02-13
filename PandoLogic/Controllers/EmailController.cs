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
            ViewBag.Title = "Welcome to BizSprout";
            ViewBag.Teaser = "Short Teaser";
            ViewBag.H1 = "Header Text";
            ViewBag.Lead = "Lead Test Over Here";
            ViewBag.Body = "Body Text Down Here";
            ViewBag.Link = "http://google.com";
            ViewBag.LinkText = "Click to Verify E-Mail Address";

            return View();
        }

        public ActionResult Invite()
        {
            ViewBag.Title = "You're Invited to BizSprout";
            ViewBag.Teaser = "{0} has invited you to collaborate on BizSprout";
            ViewBag.H1 = "You're Invited!";
            ViewBag.Lead = "Get Focused. Get Results. Collaborate.";
            ViewBag.Body = "You're invited to connect and collaborate with your team. BizSprout will empower you to plan and achieve in your business.";
            ViewBag.Link = "http://google.com";
            ViewBag.LinkText = "Click to Accept Invitation";

            return View();
        }
    }
}