using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class Chat : BaseHub
    {
        #region Fields

        // Mapping from chatRoomId to Room object
        static Dictionary<string, Room> _Rooms = new Dictionary<string, Room>();

        // Mapping from connectionId to FullName
        static Dictionary<string, string> _Names = new Dictionary<string, string>();

        static Dictionary<string, string> Names
        {
            get { return _Names; }
        }

        static Dictionary<string, Room> Rooms
        {
            get { return _Rooms; }
        }

        #endregion

        #region Inner Types

        public class UserInfo
        {
            public UserInfo(ApplicationUser user, string connectionId)
            {
                this.AvatarUrl = user.AvatarOrDefaultUrl;
                this.UserName = user.FullName;

                // TODO: Figure out how to properly make UrlHelper happy
                // UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);
                // msg.UserUrl = url.Action("Details", "Users", new { id = user.Id });

                this.UserUrl = string.Format("/Users/Details/{0}", user.Id);

                this.ConnectionId = connectionId;
            }

            public string UserUrl { get; set; }
            public string UserName { get; set; }
            public string AvatarUrl { get; set; }
            public string ConnectionId { get; set; }
        }

        public class Room
        {
            public Room(string id)
            {
                Id = id;
                Users = new Dictionary<string, UserInfo>();
            }

            public string Id { get; set; }

            // ConnectionId to UserInfo
            public Dictionary<string, UserInfo> Users { get; set; }

            public void AddUser(UserInfo user)
            {
                if (Users.ContainsKey(user.ConnectionId))
                    return;

                Users[user.ConnectionId] = user;
            }

            public void RemoveUser(string connectionId)
            {
                if (!Users.ContainsKey(connectionId))
                    return;

                Users.Remove(connectionId);
            }

            public bool HasUserWithConnectionId(string connectionId)
            {
                return Users.ContainsKey(connectionId);
            }
        }

        public class Response
        {
            public Response(string msg, string roomId)
            {
                Date = DateTime.Now.ToString("t");
                Message = msg;
                ChatRoomId = roomId;
            }

            public string Message { get; set; }
            public string Date { get; set; }
            public string ChatRoomId { get; set; }
        }

        public class RoomResponse : Response
        {
            public RoomResponse(Room room)
                : base("", room.Id)
            {
                ChatRoomId = room.Id;

                Users = new List<UserInfo>();
                foreach(UserInfo user in room.Users.Values)
                {
                    Users.Add(user);
                }
            }

            public List<UserInfo> Users { get; set; }
        }

        public class UserResponse : Response
        {
            public UserResponse(string msg, string roomId, ApplicationUser user)
                : base(msg, roomId)
            {
                this.AvatarUrl = user.AvatarOrDefaultUrl;
                this.UserName = user.FullName;

                // TODO: Figure out how to properly make UrlHelper happy
                // UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);
                // msg.UserUrl = url.Action("Details", "Users", new { id = user.Id });

                this.UserUrl = string.Format("/Users/Details/{0}", user.Id);
            }

            public string UserUrl { get; set; }
            public string UserName { get; set; }
            public string AvatarUrl { get; set; }
        }

        protected Room GetRoomForId(string id)
        {
            if (Rooms.ContainsKey(id))
            {
                return Rooms[id];
            }
            else
            {
                Room room = new Room(id);
                Rooms.Add(id, room);
                return room;
            }
        }

        #endregion

        #region Public Methods (Mapped to JavaScript)

        public async Task Join(string chatRoomId, string userId)
        {
            // Build a join message for the joining user
            ApplicationUser currentUser = this.Db.Users.Find(userId);
            UserResponse response = new UserResponse(string.Format("You have joined", currentUser.FullName), chatRoomId, currentUser);
            this.Clients.Caller.receiveMessage(response);

            // Update the current room context
            Room room = GetRoomForId(chatRoomId);
            room.AddUser(new UserInfo(currentUser, Context.ConnectionId));
            await Groups.Add(this.Context.ConnectionId, chatRoomId);

            // Update the list of users for everyone in this room
            RoomResponse usersResponse = new RoomResponse(room);
            this.Clients.Group(chatRoomId).receiveUsers(usersResponse);

            // Tell everyone else the current user joined
            UserResponse otherResponse = new UserResponse(string.Format("{0} has joined", currentUser.FullName), chatRoomId, currentUser);
            this.Clients.OthersInGroup(chatRoomId).receiveMessage(otherResponse);
        }

        public void Send(string chatRoomId, string userId, string message)
        {
            ApplicationUser currentUser = this.Db.Users.Find(userId);

            UserResponse response = new UserResponse(message, chatRoomId, currentUser);

            Clients.Group(chatRoomId).receiveMessage(response);
        }

        public void Leave(string chatRoomId, string userId)
        {
            // Notify everyone else this user has left
            ApplicationUser currentUser = this.Db.Users.Find(userId);
            string message = string.Format("{0} has left", currentUser.FullName);
            UserResponse response = new UserResponse(message, chatRoomId, currentUser);
            Clients.OthersInGroup(chatRoomId).receiveMessage(response);

            // Update the current room context
            Room room = GetRoomForId(chatRoomId);
            room.RemoveUser(Context.ConnectionId);
            Groups.Remove(this.Context.ConnectionId, chatRoomId);

            // Update the list of users for everyone in this room
            RoomResponse usersResponse = new RoomResponse(room);
            this.Clients.Group(chatRoomId).receiveUsers(usersResponse);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            foreach(Room room in Rooms.Values)
            {
                if(room.HasUserWithConnectionId(Context.ConnectionId))
                {
                    room.RemoveUser(Context.ConnectionId);
                    RoomResponse usersResponse = new RoomResponse(room);
                    this.Clients.Group(room.Id).receiveUsers(usersResponse);
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        #endregion
    }
}