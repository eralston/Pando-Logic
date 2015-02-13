using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

using PandoLogic.Models;

namespace PandoLogic
{
    public class EmailTemplates
    {
        const string FromEmailAddress = "Support@PandoLogic.Com";

        /// <summary>
        /// Sends an e-mail to the target user
        /// </summary>
        /// <param name="targetUser"></param>
        /// <param name="controller"></param>
        /// <param name="subject"></param>
        /// <param name="emailViewName"></param>
        /// <param name="model"></param>
        static void SendEmail(string toEmail, Controller controller, string subject, 
                                    string emailViewName, object model = null)
        {
            string body = controller.RenderRazorViewToString(emailViewName, model);

            MailMessage message = new MailMessage();
            message.SetFrom(FromEmailAddress);
            message.SetBody(body, body);
            message.AddTo(toEmail);
            message.Subject = subject;

            message.SendMailSmtp();

            System.Diagnostics.Trace.TraceInformation("Sent e-mail to '{0}' with subject '{1}'", toEmail, subject);
        }

        /// <summary>
        /// Sends a welcome e-mail to the given user, using the given controller to render the e-mail view
        /// </summary>
        /// <param name="targetUser"></param>
        /// <param name="controller"></param>
        public static void SendWelcomeEmail(ApplicationUser targetUser, Controller controller)
        {
            string viewName = "~/Views/Email/Generic.cshtml";
            string subject = "Welcome to BizSprout";

            ApplyWelcomeEmailToViewBag(targetUser, controller);

            SendEmail(targetUser.Email, controller, subject, viewName);
        }

        public static void ApplyWelcomeEmailToViewBag(ApplicationUser targetUser, Controller controller)
        {
            controller.ViewBag.Title = "Welcome to BizSprout";
            controller.ViewBag.Teaser = "Almost finished, please verify your e-mail by clicking the link below";
            controller.ViewBag.H1 = "Almost finished....";
            controller.ViewBag.Lead = "Confirm Your E-Mail to Start Using BizSprout";
            controller.ViewBag.Body = "We need you to confirm your email address. To complete the subscription process, please click the link below";
            controller.ViewBag.Link = string.Format("http://{0}/Home/Verify?invite={1}", Settings.SiteUrl, targetUser.VerificationInvite.Value);
            controller.ViewBag.LinkText = "Click to Verify E-Mail Address";
        }

        public static void SendInviteEmail(ApplicationUser sender, Controller controller, MemberInvite invite)
        {
            controller.ViewBag.Title = "You're Invited to BizSprout";
            controller.ViewBag.Teaser = string.Format("{0} has invited you to collaborate on BizSprout", sender.FullName);
            controller.ViewBag.H1 = "You're Invited!";
            controller.ViewBag.Lead = "Get Focused. Get Results. Collaborate.";
            controller.ViewBag.Body = string.Format("{0} has invited you to connect and collaborate on BizSprout. BizSprout will empower you to plan and achieve in your business. Please accept this invitation by clicking below.", sender.FullName);
            controller.ViewBag.Link = string.Format("http://{0}/Home/AcceptInvite?invite={1}", Settings.SiteUrl, invite.Invite.Value);
            controller.ViewBag.LinkText = "Click to Accept Invitation";

            string viewName = "~/Views/Email/Invite.cshtml";
            string subject = "You're Invited to BizSprout";

            SendEmail(invite.Email, controller, subject, viewName);
        }
    }
}