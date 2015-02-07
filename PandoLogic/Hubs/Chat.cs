using System;
using System.Collections.Generic;
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
    /// </summary>
    public class ChatSessionInfo
    {
        public string ChatRoomId { get; set; }
        public string Title { get; set; }
    }

    public class ChatUsersInfo
    {
        public ChatUsersInfo() { }
        public ChatUsersInfo(IEnumerable<ChatUserEntity> users, string chatRoomId)
        {
            this.ChatRoomId = chatRoomId;
            this.Users = users;
        }

        public string ChatRoomId { get; set; }
        public IEnumerable<ChatUserEntity> Users { get; set; }
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
        /// Adds the given user to the given room
        /// As a side-effect, this will update all people currently in the room about the user's entrance
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        private async Task AddUserToRoom(string chatRoomId, ApplicationUser currentUser)
        {
            // Update the list of users for everyone in this room
            await Storage.AddUserToRoomAsync(chatRoomId, Context.ConnectionId, currentUser);
            IEnumerable<ChatUserEntity> users = await Storage.GetUsersForRoomAsync(chatRoomId);
            ChatUsersInfo usersInfo = new ChatUsersInfo(users, chatRoomId);
            Clients.Group(chatRoomId).receiveUsers(usersInfo);

            // Send this user the history of messages
            IEnumerable<ChatMessageEntity> history = await Storage.GetMessagesForRoomAsync(chatRoomId);
            Clients.Caller.receiveHistory(history);

            // Tell everyone else the current user joined
            string systemMessage = string.Format("{0} has joined", currentUser.FullName);
            ChatMessageEntity entity = await Storage.AddMessageToRoomAsync(chatRoomId, currentUser.Id, systemMessage, ChatMessageEntity.MessageType.System);
            Clients.Group(chatRoomId).receiveMessage(entity);
        }

        /// <summary>
        /// Asynchrously removes the given user from the given room
        /// As a side-effect, this will send a message to that room indicating the user has left
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        private async Task RemoveUserFromRoom(string chatRoomId, ApplicationUser currentUser)
        {
            // Update the list of users for everyone in this room
            await Storage.RemoveUserFromRoomAsync(chatRoomId, Context.ConnectionId);
            IEnumerable<ChatUserEntity> chat = await Storage.GetUsersForRoomAsync(chatRoomId);
            this.Clients.Group(chatRoomId).receiveUsers(chat);

            // Tell everyone else the current user joined
            string systemMessage = string.Format("{0} has left", currentUser.FullName);
            ChatMessageEntity entity = await Storage.AddMessageToRoomAsync(chatRoomId, currentUser.Id, systemMessage, ChatMessageEntity.MessageType.System);
            this.Clients.Group(chatRoomId).receiveMessage(entity);
        }

        #endregion

        #region Public Methods (Mapped to JavaScript)

        /// <summary>
        /// Called when a user joins a room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task Join(string chatRoomId, string userId)
        {
            // Add this connection to the given groups
            await Groups.Add(Context.ConnectionId, chatRoomId);

            // Pull the current user information
            // TODO: Make this auth driven instead of an argument
            ApplicationUser currentUser = this.Db.Users.Find(userId);

            await AddUserToRoom(chatRoomId, currentUser);
        }

        /// <summary>
        /// Called when a user wants to send a message to everyone in the room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        public async void Send(string chatRoomId, string userId, string message)
        {
            ApplicationUser currentUser = this.Db.Users.Find(userId);

            // Tell everyone about the current message
            ChatMessageEntity entity = await Storage.AddMessageToRoomAsync(chatRoomId, currentUser.Id, message, ChatMessageEntity.MessageType.User);
            Clients.Group(chatRoomId).receiveMessage(entity);
        }

        /// <summary>
        /// Called when the user gracefully logs out of a room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        public async void Leave(string chatRoomId, string userId)
        {
            // Take this user out of the group
            await Groups.Remove(Context.ConnectionId, chatRoomId);

            // Build a leave message for the joining user
            ApplicationUser currentUser = this.Db.Users.Find(userId);

            await RemoveUserFromRoom(chatRoomId, currentUser);
        }

        /// <summary>
        /// Called when a user disconnects from the chat
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            string connectionId = Context.ConnectionId;
            IEnumerable<ChatUserEntity> usersWithConnectionId = Storage.GetUsersForConnectionId(connectionId);
            foreach (ChatUserEntity user in usersWithConnectionId)
            {
                Storage.DeleteUser(user);
            }

            return base.OnDisconnected(stopCalled);
        }

        #endregion
    }
}