using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

using Masticore;

using PandoLogic.Models;

namespace PandoLogic
{
    public enum ChatMessageType
    {
        User,
        System
    }

    /// <summary>
    /// ViewModel for sending down the vital data of a ChatMessageEntity down to the client
    /// </summary>
    public class ChatMessageViewModel
    {
        public ChatMessageViewModel(ChatMessageEntity message, string roomId)
        {
            this.Message = message.Message;
            this.Type = message.Type;
            this.CreatedDateUtc = message.CreatedDateUtc;
            this.UserId = message.UserId;
            this.ChatRoomId = roomId;
        }

        public string ChatRoomId { get; set; }

        /// <summary>
        /// The text of the chat
        /// </summary>
        public string Message { get; set; }

        public ChatMessageType Type { get; set; }

        public DateTime CreatedDateUtc { get; set; }

        public string CreatedDateString
        {
            get
            {
                return CreatedDateUtc.ToString();
            }
        }

        public string UserId { get; set; }
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
        /// <param name="chatRoomId"></param>
        /// <param name="messageId"></param>
        public ChatMessageEntity(string chatRoomId, string userId, string message, ChatMessageType type = ChatMessageType.User)
        {
            // One partition per room
            this.PartitionKey = chatRoomId;

            // One row per chat message
            // This will cause them to sort ascending by time when queried back out
            this.RowKey = string.Format("{0:D19}", DateTime.UtcNow.Ticks);

            // The configurable fields for this record
            this.TypeId = (int)type;
            this.Message = message;
            this.UserId = userId;
        }

        /// <summary>
        /// Gets or sets the chat room ID for this message (this is the partition key)
        /// NOTE: This is NOT persisted to table storage because it's just a remapping of PartitionKey
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

        /// <summary>
        /// The UTC datetime when this message was made
        /// NOTE: This is theoretically redundant to Timestamp, but in practice easier to handle in the presentation layer
        /// </summary>
        public DateTime CreatedDateUtc { get; set; }

        public string UserId { get; set; }

        public int TypeId { get; set; }

        public ChatMessageType Type
        {
            get
            {
                return (ChatMessageType)this.TypeId;
            }
        }
    }

    /// <summary>
    /// Implements storage for user messages
    /// NOTE: This does NOT implement security of any kind, that is for the layer above
    /// </summary>
    public class ChatStorageManager : TableStorageManager
    {
        /// <summary>
        /// Identifier for the table containing all messages
        /// </summary>
        protected const string RoomMessagesTableId = "ChatStorageManagerRoomMessagesTableId";

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
        /// Async adds a chat message to the given room, from the given user
        /// </summary>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<ChatMessageEntity> AddMessageToRoomAsync(string chatRoomId, string userId, string message, ChatMessageType type = ChatMessageType.User)
        {
            ChatMessageEntity chatMessageEntity = new ChatMessageEntity(chatRoomId, userId, message, type);
            CloudTable table = await GetRoomMessagesTableAsync();
            TableResult result = await table.InsertOrReplaceEntityAsync(chatMessageEntity);
            return chatMessageEntity;
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