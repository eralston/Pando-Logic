using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

using System.Net.Mail;
using System.Net.Mime;

/// <summary>
/// A set of extensions that simplifies interaction with the MailMessage class
/// </summary>
public static class MailExtensions
{
    /// <summary>
    /// Adds a to Address to the message, optionally setting the display name
    /// </summary>
    /// <param name="message"></param>
    /// <param name="address"></param>
    /// <param name="displayName"></param>
    public static void AddTo(this MailMessage message, string address, string displayName = null)
    {
        message.To.Add(new MailAddress(address, displayName));
    }

    /// <summary>
    /// Sets the from address for the message, optionally setting the display name
    /// </summary>
    /// <param name="message"></param>
    /// <param name="address"></param>
    public static void SetFrom(this MailMessage message, string address, string displayName = null)
    {
        message.From = new MailAddress(address, displayName);
    }

    /// <summary>
    /// Adds an attachment to the e-mail
    /// </summary>
    /// <param name="message"></param>
    /// <param name="bytes"></param>
    /// <param name="name"></param>
    /// <param name="contentType"></param>
    public static void AddAttachment(this MailMessage message, byte[] bytes, string name, string contentType)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            message.Attachments.Add(new Attachment(stream, name, contentType));
        }
    }

    /// <summary>
    /// Sets the body of the message, optionally apply an html version
    /// </summary>
    /// <param name="message"></param>
    /// <param name="text"></param>
    /// <param name="html"></param>
    public static void SetBody(this MailMessage message, string text, string html = null)
    {
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
        if (!string.IsNullOrEmpty(html))
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
    }

    /// <summary>
    /// Sends the given message using the SMTP settings
    /// </summary>
    /// <param name="message"></param>
    /// <param name="smtpName"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public static void SendMailSmtp(this MailMessage message, string smtpName, string username, string password)
    {
        using (SmtpClient smtpClient = new SmtpClient(smtpName, Convert.ToInt32(587)))
        {
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(username, password);
            smtpClient.Credentials = credentials;

            smtpClient.Send(message);
        }
    }

    /// <summary>
    /// Sends the given message using the SMTP settings, using the settings in the web.config
    /// </summary>
    /// <param name="message"></param>
    /// <param name="smtpName"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public static void SendMailSmtp(this MailMessage message)
    {
        using (SmtpClient smtpClient = new SmtpClient())
        {
            smtpClient.Send(message);
        }
    }
}