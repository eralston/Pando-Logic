using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Bookmark for the user, enabling them to save a working list of strategies
    /// </summary>
    public class StrategyBookmark : ModelBase, IUserOwnedModel
    {
        // To-One on ApplicationUser
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // To-One on Strategy
        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }
        public virtual Strategy Strategy { get; set; }
    }

    public static class StrategyBookmarkExtensions
    {
        public static StrategyBookmark Create(this DbSet<StrategyBookmark> bookmarks, string userId, int strategyId)
        {
            StrategyBookmark bookmark = bookmarks.Create();

            bookmark.CreatedDateUtc = DateTime.UtcNow;
            bookmark.UserId = userId;
            bookmark.StrategyId = strategyId;

            bookmarks.Add(bookmark);

            return bookmark;
        }

        public static async Task<bool> IsBookmarked(this DbSet<StrategyBookmark> bookmarks, string userId, int strategyId)
        {
            StrategyBookmark bookmark = await FindBookmarkByUserAndStrategyAsync(bookmarks, userId, strategyId);
            bool isBookmarked = bookmark != null;
            return isBookmarked;
        }

        public static async Task<StrategyBookmark> FindBookmarkByUserAndStrategyAsync(this DbSet<StrategyBookmark> bookmarks, string userId, int strategyId)
        {
            StrategyBookmark bookmark = await bookmarks.Where(sb => sb.UserId == userId && sb.StrategyId == strategyId && !sb.Strategy.IsDeleted).FirstOrDefaultAsync();
            return bookmark;
        }

        public static IQueryable<StrategyBookmark> WhereUser(this DbSet<StrategyBookmark> bookmarks, string userId)
        {
            return bookmarks.Where(b => b.UserId == userId && !b.Strategy.IsDeleted).Include(b => b.Strategy).OrderBy(b => b.CreatedDateUtc);
        }

        public static async Task<IEnumerable<Strategy>> StrategiesWhereBookmarkedByUserAsync(this DbSet<StrategyBookmark> bookmarks, string userId)
        {
            StrategyBookmark[] marks = await bookmarks.WhereUser(userId).ToArrayAsync();
            List<Strategy> strategies = new List<Strategy>();
            foreach (StrategyBookmark mark in marks)
            {
                strategies.Add(mark.Strategy);
            }

            return strategies;
        }
    }
}