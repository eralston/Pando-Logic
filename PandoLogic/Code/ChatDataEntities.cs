using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

using PandoLogic.Models;

namespace PandoLogic.Code
{
    /// <summary>
    /// An Azure Table Storage entity for holding onto the connected user information
    /// There is one partition for each chat instance in Pando Logic
    /// There is one row in each partition for each
    /// This holds all data relevant to users in the chat rooms
    /// They should be inserted when users enter a room
    /// They should be deleted when a user exits a room
    /// They should be read when a user enters, telling them about the other users in the room
    /// </summary>
    public class ChatConnectionEntity : TableEntity
    {
        /// <summary>
        /// Required parameter-less constructor
        /// </summary>
        public ChatConnectionEntity() { }

        /// <summary>
        /// Constructor for making a new instance (before insert/update)
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userName"></param>
        public ChatConnectionEntity(string chatId, ApplicationUser user, string connectionId)
        {
            this.PartitionKey = chatId;
            this.RowKey = user.UserName;

            if (user.Avatar != null)
                this.AvatarUrl = user.Avatar.Url;

            this.UserName = user.FullName;
            this.UserUrl = this.UserUrl = string.Format("/Users/Details/{0}", user.Id);

            this.ConnectionId = connectionId;
        }

        #region User Display Fields (Copy from user info in the system)

        public string UserUrl { get; set; }
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }
        public string ConnectionId { get; set; }

        #endregion
    }

    /// <summary>
    /// An azure table storage entity for holding onto the connnection
    /// They are partitioned on the chat room ID (each "Room" in the system)
    /// They are row keyed by unique GUID to identify it
    /// They should be inserted when a user posts a message to a rom
    /// They should be read when a user joins the chat and receives the backlog of messages
    /// </summary>
    public class ChatMessageEntity : TableEntity
    {
        /// <summary>
        /// Required parameter-less constructor
        /// </summary>
        public ChatMessageEntity() { }

        /// <summary>
        /// Constructor for new chat message
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        public ChatMessageEntity(string chatId, string messageId, string message)
        {
            this.PartitionKey = chatId;
            this.RowKey = messageId;
            this.Message = message;
            this.SentDate = DateTime.Now;
        }

        /// <summary>
        /// The text of the chat
        /// </summary>
        public string Message { get; set; }

        public DateTime SentDate { get; set; }
    }
}