using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNet.SignalR;

using PandoLogic;
using PandoLogic.Models;

namespace PandoLogic.Hubs
{
    /// <summary>
    /// Small ViewModel for presenting chat setup data to JavaScript
    /// This is used in the CSHTML template
    /// </summary>
    public class ChatSessionInfo
    {
        public string ChatRoomId { get; set; }
        public string Title { get; set; }
        public bool IsAnnouncedJoinAndLeave { get; set; }
    }

    /// <summary>
    /// ViewModel for passing down user list for a single room
    /// </summary>
    public class ChatOccupantInfo
    {
        public ChatOccupantInfo(string roomId, ApplicationUserViewModel[] userArray)
        {
            this.ChatRoomId = roomId;
            this.Users = userArray;
        }

        public string ChatRoomId { get; set; }
        public ApplicationUserViewModel[] Users { get; set; }
    }

    /// <summary>
    /// SignalR hub for managing the chat hub
    /// </summary>
    public class Chat : BaseHub
    {
        #region Fields & Properties

        ChatStorageManager _chatStorage = null;

        /// <summary>
        /// Gets the storage manager for this chat hub, which persists state on behalf of this
        /// </summary>
        protected ChatStorageManager Storage
        {
            get
            {
                _chatStorage = _chatStorage ?? new ChatStorageManager();
                return _chatStorage;
            }
        }

        #endregion

        #region Private Methods (Not available in Js)

        /// <summary>
        /// Sends out an updates to everyone in the given room informing them who is currently in it according to the DB
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <returns></returns>
        private async Task UpdateOccupantListForRoom(string chatRoomId)
        {
            ChatUser[] chatUsers = await Db.ChatUsers.WhereInRoom(chatRoomId).ToArrayAsync();
            ApplicationUserViewModel[] userViewModels = chatUsers.ToApplicationUserViewModels();
            ChatOccupantInfo occupantInfo = new ChatOccupantInfo(chatRoomId, userViewModels);
            Clients.Group(chatRoomId).receiveUsers(occupantInfo);
        }

        #endregion

        #region Public Methods (Mapped to JavaScript)

        /// <summary>
        /// Called when a user joins a room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task Join(string chatRoomId, string userId, bool isAnnounced)
        {
            // Pull the current user information
            // TODO: Make this auth driven instead of an argument
            ApplicationUser currentUser = this.Db.Users.Find(userId);

            // Add an entry to SignalR group for this connection
            await Groups.Add(Context.ConnectionId, chatRoomId);

            // Add an entry to the room for the current user
            ChatUser user = Db.ChatUsers.Create(chatRoomId, Context.ConnectionId, currentUser.Id);
            await Db.SaveChangesAsync();

            // Tell everyone who is in the room
            await UpdateOccupantListForRoom(chatRoomId);

            // Send this user the history of messages
            IEnumerable<ChatMessageEntity> history = await Storage.GetMessagesForRoomAsync(chatRoomId);
            Clients.Caller.receiveHistory(history);

            if(isAnnounced)
            {
                // Tell everyone else the current user joined
                string systemMessage = string.Format("{0} has joined", currentUser.FullName);
                // NOTE: Do NOT persist join messages, just make an entity for sending
                ChatMessageEntity entity = new ChatMessageEntity(chatRoomId, userId, systemMessage, ChatMessageType.System);
                Clients.Group(chatRoomId).receiveMessage(entity);
            }
        }

        /// <summary>
        /// Called when a user wants to send a message to everyone in the room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        public async Task SendMessage(string chatRoomId, string userId, string message)
        {
            ApplicationUser currentUser = this.Db.Users.Find(userId);
            // Tell everyone about the current message
            ChatMessageEntity entity = await Storage.AddMessageToRoomAsync(chatRoomId, currentUser.Id, message, ChatMessageType.User);
            ChatMessageViewModel messageViewModel = new ChatMessageViewModel(entity, chatRoomId);
            Clients.Group(chatRoomId).receiveMessage(messageViewModel);
        }

        /// <summary>
        /// Called when the user gracefully logs out of a room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        public async Task Leave(string chatRoomId, string userId, bool isAnnounced)
        {
            // Take this user out of the group
            await Groups.Remove(Context.ConnectionId, chatRoomId);

            // Update the list of users for everyone in this room
            Db.ChatUsers.RemoveForUserAndRoom(chatRoomId, userId);
            await Db.SaveChangesAsync();

            // Tell everyone the room occupants have changed
            await UpdateOccupantListForRoom(chatRoomId);

            if(isAnnounced)
            {
                // Tell everyone else the current user left
                ApplicationUser currentUser = this.Db.Users.Find(userId);
                string systemMessage = string.Format("{0} has left", currentUser.FullName);
                // NOTE: Do NOT persist leave messages, just make an entity for sending
                ChatMessageEntity entity = new ChatMessageEntity(chatRoomId, userId, systemMessage, ChatMessageType.System);
                this.Clients.Group(chatRoomId).receiveMessage(entity);
            }
        }

        /// <summary>
        /// Called when a user disconnects from the chat
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            string connectionId = Context.ConnectionId;
            Db.ChatUsers.RemoveAllWithConnectionId(connectionId);
            Db.SaveChanges();

            return base.OnDisconnected(stopCalled);
        }

        #endregion
    }
}