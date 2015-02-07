using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace PandoLogic
{
    /// <summary>
    /// A class with extensions to make working with tables easier
    /// </summary>
    public static class CloudTableExtensions
    {
        /// <summary>
        /// Asynchronously inserts the given entity into the table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Task<TableResult> InsertEntityAsync(this CloudTable table, TableEntity entity)
        {
            // Create the TableOperation that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(entity);

            // Execute the operation asynchronously
            return table.ExecuteAsync(insertOperation);
        }

        /// <summary>
        /// Inserts or replaces the given entity in the table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Task<TableResult> InsertOrReplaceEntityAsync(this CloudTable table, TableEntity entity)
        {
            // Create the InsertOrReplace TableOperation
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            // Execute the operation asynchronously
            return table.ExecuteAsync(insertOrReplaceOperation);
        }

        /// <summary>
        /// Retrieves a single entity with the given partition and rowKey
        /// NOTE: This will return null if the entity was not found
        /// </summary>
        /// <typeparam name="EntityType"></typeparam>
        /// <param name="table"></param>
        /// <param name="partition"></param>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        public static async Task<EntityType> RetrieveEntityAsync<EntityType>(this CloudTable table, string partition, string rowKey) where EntityType : TableEntity
        {
            // Setup the retrieve
            TableOperation retrieveOperation = TableOperation.Retrieve<EntityType>(partition, rowKey);

            // Execute the operation async
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            EntityType entity = (EntityType)retrievedResult.Result;

            return entity;
        }

        /// <summary>
        /// Retrieves all entities in the given partition from the given table
        /// </summary>
        /// <typeparam name="EntityType"></typeparam>
        /// <param name="table"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        public static IEnumerable<EntityType> RetrieveAllEntitiesInPartitionAsync<EntityType>(this CloudTable table, string partitionId) where EntityType : TableEntity, new()
        {
            TableQuery<EntityType> query = new TableQuery<EntityType>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionId));

            return table.ExecuteQuery(query);
        }

        /// <summary>
        /// Retrieves all entities with the given rowkey, regardless of partition
        /// </summary>
        /// <typeparam name="EntityType"></typeparam>
        /// <param name="table"></param>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        public static IEnumerable<EntityType> RetrieveEntitiesInAllPartitionWithRowKeyAsync<EntityType>(this CloudTable table, string rowKey) where EntityType : TableEntity, new()
        {
            TableQuery<EntityType> query = new TableQuery<EntityType>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));

            return table.ExecuteQuery(query);
        }

        /// <summary>
        /// DAsynchronously delete the given entity from the given table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Task<TableResult> DeleteEntityAsync(this CloudTable table, TableEntity entity)
        {
            // Create the Delete TableOperation
            TableOperation deleteOperation = TableOperation.Delete(entity);

            // Execute the operation asynchronously
            return table.ExecuteAsync(deleteOperation);
        }

        /// <summary>
        /// Delete the given entity from the given table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static TableResult DeleteEntity(this CloudTable table, TableEntity entity)
        {
            // Create the Delete TableOperation
            TableOperation deleteOperation = TableOperation.Delete(entity);

            // Execute the operation asynchronously
            return table.Execute(deleteOperation);
        }
    }

    /// <summary>
    /// A helper class for managing Azure Table storage
    /// http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/
    /// </summary>
    public class TableStorageManager : StorageManager
    {
        CloudTableClient _tableClient = null;
        /// <summary>
        /// Gets the TableClient instance for this storage manager (lazy instantiating it and only once)
        /// </summary>
        protected CloudTableClient TableClient
        {
            get
            {
                _tableClient = _tableClient ?? Account.CreateCloudTableClient();
                return _tableClient;
            }
        }

        /// <summary>
        /// Gets a table reference for the given table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        protected CloudTable GetTableReference(string tableName)
        {          
            // Get a reference to the table
            CloudTable table = TableClient.GetTableReference(tableName);

            return table;
        }

        /// <summary>
        /// Asynchronously retrieves a reference to the table with the given table name
        /// Will create if not exists
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<CloudTable> GetTableAsync(string tableName)
        {
            CloudTable table = GetTableReference(tableName);

            // Create if not already there
            await table.CreateIfNotExistsAsync();

            return table;
        }

        /// <summary>
        /// Retrieves a reference to the table with the given table name
        /// Will create if not exists
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public CloudTable GetTable(string tableName)
        {
            CloudTable table = GetTableReference(tableName);

            // Create if not already there
            table.CreateIfNotExists();

            return table;
        }
    }
}