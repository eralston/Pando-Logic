using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace PandoLogic.Code
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
        public static Task<TableResult> InsertAsync(this CloudTable table, TableEntity entity)
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
        public static Task<TableResult> InsertOrReplaceAsync(this CloudTable table, TableEntity entity)
        {
            // Create the InsertOrReplace TableOperation
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);

            // Execute the operation asynchronously
            return table.ExecuteAsync(insertOrReplaceOperation);
        }

        /// <summary>
        /// Delete the given entity from the given table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Task<TableResult> DeleteAsync(this CloudTable table, TableEntity entity)
        {
            // Create the Delete TableOperation
            TableOperation deleteOperation = TableOperation.Delete(entity);

            // Execute the operation asynchronously
            return table.ExecuteAsync(deleteOperation);
        }
    }

    /// <summary>
    /// A helper class for managing Azure Table storage
    /// http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-tables/
    /// </summary>
    public class TableStorageManager : StorageManager
    {
        public async Task<CloudTable> GetTableAsync(string tableName)
        {
            // Create the table client
            CloudTableClient tableClient = Account.CreateCloudTableClient();

            // Get a reference to the table
            CloudTable table = tableClient.GetTableReference(tableName);

            // Create if not already there
            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}