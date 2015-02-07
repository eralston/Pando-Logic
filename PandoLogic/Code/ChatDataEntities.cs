using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

using PandoLogic.Models;

namespace PandoLogic
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
    public class ChatUserEntity : TableEntity
    {
        /// <summary>
        /// Required parameter-less constructor
        /// </summary>
        public ChatUserEntity() { }

        /// <summary>
        /// Constructor for making a new instance (before insert/update)
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userName"></param>
        public ChatUserEntity(string chatRoomId, string connectionId, ApplicationUser user)
        {
            // One partition per room
            this.PartitionKey = chatRoomId;

            // One row per connection
            this.RowKey = connectionId;

            // Other user fields
            this.UserId = user.Id;
            this.UserName = user.FullName;
            this.AvatarUrl = user.AvatarOrDefaultUrl;
            this.UserUrl = string.Format("/Users/Details/{0}", user.Id);
        }

        /// <summary>
        /// Gets or sets the chat room ID for this user entry (this is the partition key)
        /// </summary>
        [IgnoreProperty]
        public string ChatRoomId
        {
            get
            {
                return this.PartitionKey;
            }
            set
            {
                this.PartitionKey = value;
            }
        }

        #region User Display Fields (Copy from user info in the system)

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserUrl { get; set; }
        public string AvatarUrl { get; set; }

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
        public enum MessageType
        {
            User,
            System
        }

        /// <summary>
        /// Required parameter-less constructor
        /// </summary>
        public ChatMessageEntity() { }

        /// <summary>
        /// Constructor for new chat message
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="messageId"></param>
        public ChatMessageEntity(string chatRoomId, string userId, string message, MessageType type = MessageType.User)
        {
            // One partition per room
            this.PartitionKey = chatRoomId;

            // One row per chat message
            // These are intrinsic to creating the message
            this.RowKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.PostDateUTC = DateTime.UtcNow;

            // The configurable fields for this record
            this.Type = type;
            this.Message = message;
            this.UserId = userId;
        }

        /// <summary>
        /// Gets or sets the chat room ID for this message (this is the partition key)
        /// </summary>
        [IgnoreProperty]
        public string ChatRoomId
        {
            get
            {
                return this.PartitionKey;
            }
            set
            {
                this.PartitionKey = value;
            }
        }

        /// <summary>
        /// The text of the chat
        /// </summary>
        public string Message { get; set; }

        public MessageType Type { get; set; }

        public DateTime PostDateUTC { get; set; }

        public string Date
        {
            get
            {
                return PostDateUTC.ToString();
            }
        }

        public string UserId { get; set; }
    }

    /// <summary>
    /// Implements storage for user messages
    /// NOTE: This does NOT implement security of any kind, that is for the layer above
    /// </summary>
    public class ChatStorageManager : TableStorageManager
    {
        /// <summary>
        /// Identifier for the list of users inside of rooms
        /// </summary>
        protected const string RoomTableId = "ChatStorageManagerRoomTableId";

        /// <summary>
        /// Identifier for the table containing all messages
        /// </summary>
        protected const string RoomMessagesTableId = "ChatStorageManagerRoomMessagesTableId";

        /// <summary>
        /// Async retrieves a table of all rooms and their connections (EG, each "Room" and its users)
        /// </summary>
        /// <returns></returns>
        CloudTable _roomTable = null;
        protected async Task<CloudTable> GetRoomTableAsync()
        {
            _roomTable = _roomTable ?? await GetTableAsync(RoomTableId);
            return _roomTable;
        }
        protected CloudTable GetRoomTable()
        {
            _roomTable = _roomTable ?? GetTable(RoomTableId);
            return _roomTable;
        }

        /// <summary>
        /// Async retrieves a table of all rooms and their messages
        /// </summary>
        /// <returns></returns>
        CloudTable _roomMessageTable = null;
        protected async Task<CloudTable> GetRoomMessagesTableAsync()
        {
            _roomMessageTable = _roomMessageTable ?? await GetTableAsync(RoomMessagesTableId);
            return _roomMessageTable;
        }
        protected CloudTable GetRoomMessagesTable()
        {
            _roomMessageTable = _roomMessageTable ?? GetTable(RoomMessagesTableId);
            return _roomMessageTable;
        }

        /// <summary>
        /// Adds a user to the given chat room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="connectionId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<ChatUserEntity> AddUserToRoomAsync(string chatRoomId, string connectionId, ApplicationUser user)
        {
            ChatUserEntity userEntity = new ChatUserEntity(chatRoomId, connectionId, user);
            CloudTable table = await GetRoomTableAsync();
            await table.InsertOrReplaceEntityAsync(userEntity);
            return userEntity;
        }

        /// <summary>
        /// Async removes a user from the given chat room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public async Task RemoveUserFromRoomAsync(string chatRoomId, string connectionId)
        {
            CloudTable table = await GetRoomTableAsync();
            ChatUserEntity entity = await table.RetrieveEntityAsync<ChatUserEntity>(chatRoomId, connectionId);
            if (entity != null)
            {
                await table.DeleteEntityAsync(entity);
            }
        }

        public void DeleteUser(ChatUserEntity user)
        {
            CloudTable table = GetRoomTable();
            table.DeleteEntity(user);
        }

        /// <summary>
        /// Async adds a chat message to the given room, from the given user
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<ChatMessageEntity> AddMessageToRoomAsync(string chatRoomId, string userId, string message, ChatMessageEntity.MessageType type = ChatMessageEntity.MessageType.User)
        {
            ChatMessageEntity chatMessageEntity = new ChatMessageEntity(chatRoomId, userId, message, type);
            CloudTable table = await GetRoomMessagesTableAsync();
            TableResult result = await table.InsertOrReplaceEntityAsync(chatMessageEntity);
            return chatMessageEntity;
        }

        /// <summary>
        /// Async retrieval of all users in a given room
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChatUserEntity>> GetUsersForRoomAsync(string chatRoomId)
        {
            CloudTable table = await GetRoomMessagesTableAsync();
            return table.RetrieveAllEntitiesInPartitionAsync<ChatUserEntity>(chatRoomId);
        }

        /// <summary>
        /// Async retrieval of all user entities for the given connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public IEnumerable<ChatUserEntity> GetUsersForConnectionId(string connectionId)
        {
            CloudTable table = GetRoomMessagesTable();
            return table.RetrieveEntitiesInAllPartitionWithRowKeyAsync<ChatUserEntity>(connectionId);
        }

        /// <summary>
        /// Async retrieval of all messages in a chat room
        /// TODO: Implement some paging strategy such that we can do chunks at a time instead of the whole thing
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ChatMessageEntity>> GetMessagesForRoomAsync(string chatRoomId)
        {
            CloudTable table = await GetRoomMessagesTableAsync();
            return table.RetrieveAllEntitiesInPartitionAsync<ChatMessageEntity>(chatRoomId);
        }
    }
}