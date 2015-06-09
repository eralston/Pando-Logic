using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Masticore;
using Microsoft.WindowsAzure.Storage.Table;

namespace PandoLogic.Models
{
    /// <summary>
    /// Repository for storing comments into Azure Table Storage
    /// </summary>
    public class ActivityRepository : TableRepositoryBase
    {
        /// <summary>
        /// Creates a repo for the given strategy ID
        /// </summary>
        /// <param name="strategyId"></param>
        /// <returns></returns>
        public static ActivityRepository CreateForStrategy(Strategy strategy)
        {
            if(strategy.CompanyId.HasValue)
            {
                return CreateForCompany(strategy.CompanyId.Value);
            }
            else
            {
                string tableName = string.Format("Strategy{0}Activity", strategy.Id);
                return new ActivityRepository(tableName);
            }
        }

        /// <summary>
        /// Creates a repo for the given company ID
        /// This is intended to contain all comments on Goals and Tasks owned by the company
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static ActivityRepository CreateForCompany(int companyId)
        {
            string tableName = string.Format("Company{0}Activity", companyId);
            return new ActivityRepository(tableName);
        }

        /// <summary>
        /// Constructor for a comment repository
        /// One per company and unique to the Activity type
        /// </summary>
        /// <param name="companyId"></param>
        protected ActivityRepository(string tableName)
            : base(tableName)
        {
        }

        /// <summary>
        /// Inserts or updates a comment associated with the given parent type (which can be any object)
        /// </summary>
        /// <typeparam name="ParentType"></typeparam>
        /// <param name="parentId"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        public Task<TableResult> InsertOrReplace<ParentType>(int parentId, Activity activity)
        {
            return this.InsertOrReplaceEntityAsync<ParentType, Activity>(parentId, activity);
        }
        
        /// <summary>
        /// Upserts a comment into a special partition for non-parented items
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public Task<TableResult> InsertOrReplace(Activity activity)
        {
            // Use a default partition for non-assigned comments
            return this.InsertOrReplaceEntityAsync<Activity, Activity>(0, activity);
        }

        /// <summary>
        /// Retrieves all activities for this company, optionally limiting the result
        /// </summary>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IList<Activity>> RetrieveAll(int? take = null)
        {
            return this.RetrieveEntitiesAsync<Activity>(take);
        }

        /// <summary>
        /// Retrieves a single activity for the given parentId and rowkey
        /// </summary>
        /// <typeparam name="ParentType"></typeparam>
        /// <param name="parentId"></param>
        /// <param name="activityRowKey"></param>
        /// <returns></returns>
        public Task<Activity> Retrieve<ParentType>(int parentId, string activityRowKey)
        {
            return this.RetrieveEntityForParentAsync<ParentType, Activity>(parentId, activityRowKey);
        }

        /// <summary>
        /// Retrieve comments associated with the given parent type and ID
        /// </summary>
        /// <typeparam name="ParentType"></typeparam>
        /// <param name="parentId"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IList<Activity>> Retrieve<ParentType>(int parentId, int? take = null)
        {
            return this.RetrieveEntitiesForParentAsync<ParentType, Activity>(parentId, take);
        }
    }
}