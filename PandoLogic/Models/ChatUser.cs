using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Model for tracking users in chat rooms
    /// These are updated continuously as user enter and leave rooms in the system
    /// </summary>
    public class ChatUser : BaseModel
    {
        public string ChatRoomId { get; set; }
        public string ConnectionId { get; set; }

        // To-One on User
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

    public static class ChatUserExtensions
    {
        /// <summary>
        /// Creates a new instance of the chat room ID
        /// </summary>
        /// <param name="users"></param>
        /// <param name="chatRoomId"></param>
        /// <param name="connectionId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static ChatUser Create(this IDbSet<ChatUser> users, string chatRoomId, string connectionId, string userId)
        {
            ChatUser newUser = users.Create();

            newUser.CreatedDateUtc = DateTime.UtcNow;

            newUser.ConnectionId = connectionId;
            newUser.ChatRoomId = chatRoomId;
            newUser.UserId = userId;

            users.Add(newUser);

            return newUser;
        }

        /// <summary>
        /// Returns all chat user entries for the given chat room, including the user and user's avatar
        /// </summary>
        /// <param name="users"></param>
        /// <param name="chatRoomId"></param>
        /// <returns></returns>
        public static IQueryable<ChatUser> WhereInRoom(this DbSet<ChatUser> users, string chatRoomId)
        {
            return users.Where(u => u.ChatRoomId == chatRoomId).Include(u => u.User).Include(u => u.User.Avatar);
        }

        /// <summary>
        /// Removes all entries for the given chatRoomId and user ID
        /// NOTE: You must still call SaveChanges on the context for the changes to stay
        /// </summary>
        /// <param name="users"></param>
        /// <param name="chatRoomId"></param>
        /// <param name="userId"></param>
        public static void RemoveForUserAndRoom(this DbSet<ChatUser> users, string chatRoomId, string userId)
        {
            users.RemoveRange(users.Where(u => u.UserId == userId && u.ChatRoomId == chatRoomId));
        }

        /// <summary>
        /// Removes all ChatUser entries for the given connectionId
        /// NOTE: You must still call saveChanges on the context for the changes to stay
        /// </summary>
        /// <param name="users"></param>
        /// <param name="connectionId"></param>
        public static void RemoveAllWithConnectionId(this DbSet<ChatUser> users, string connectionId)
        {
            users.RemoveRange(users.Where(u => u.ConnectionId == connectionId));
        }

        /// <summary>
        /// Converts a set of ChatUsers into view models for the UI to consume
        /// </summary>
        /// <param name="chatUserCollection"></param>
        /// <returns></returns>
        public static ApplicationUserViewModel[] ToApplicationUserViewModels(this IEnumerable<ChatUser> chatUserCollection)
        {
            List<ApplicationUserViewModel> viewModels = new List<ApplicationUserViewModel>();

            foreach(ChatUser chatUser in chatUserCollection)
            {
                viewModels.Add(new ApplicationUserViewModel(chatUser.User));
            }

            return viewModels.ToArray();
        }
    }
}