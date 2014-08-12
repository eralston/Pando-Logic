using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web;

using Microsoft.AspNet.SignalR;

using PandoLogic.Models;

namespace PandoLogic.Hubs
{
    public class ChatInfo
    {
        public string ChatRoomId { get; set; }
        public string Title { get; set; }
    }

    /// <summary>
    /// SignalR hub for managing the chat hub
    /// </summary>
    public class Chat : Hub
    {
        public class Message
        {
            public string ChatRoomId { get; set; }
            public string UserUrl { get; set; }
            public string UserName { get; set; }
            public string AvatarUrl { get; set; }
            public string Date { get; set; }
            public string Body { get; set; }
        }

        public void Send(string chatRoomId, string userId, string message)
        {
            ApplicationDbContext context = new ApplicationDbContext();
            ApplicationUser user = context.Users.Find(userId);

            Message msg = new Message();
            msg.ChatRoomId = chatRoomId;
            msg.Date = DateTime.Now.ToString("t");
            msg.Body = message;
            msg.AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? "/Content/images/user-icon.png" : user.AvatarUrl;
            msg.UserName = user.FullName;

            UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            msg.UserUrl = url.Action("Details", "Users", new { id = user.Id });

            Clients.All.receiveMessage(msg);
        }
    }
}