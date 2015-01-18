using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PandoLogic
{
    /// <summary>
    /// Simple extensions 
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        /// Pulls the reference for the given blob and uploads the stream to it asynchronously
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="uploadStream"></param>
        /// <returns></returns>
        public static Task UploadBlobAsync(this CloudBlobContainer container, string blobName, Stream uploadStream)
        {
            System.Diagnostics.Trace.TraceInformation("Uploading azure storage blob '{0}' to container '{1}'", blobName, container.Name);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            return blockBlob.UploadFromStreamAsync(uploadStream);
        }

        /// <summary>
        /// Pulls the reference for the given blob and downloads it asynchronously to the given stream
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="downloadStream"></param>
        /// <returns></returns>
        public static Task DownloadBlobAsync(this CloudBlobContainer container, string blobName, Stream downloadStream)
        {
            System.Diagnostics.Trace.TraceInformation("Downloading azure storage blob '{0}' from container '{1}'", blobName, container.Name);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            return blockBlob.DownloadToStreamAsync(downloadStream);
        }

        /// <summary>
        /// Asynchronously checks if the given filename exists in the containers
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public static Task<bool> HasBlobAsync(this CloudBlobContainer container, string blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            return blockBlob.ExistsAsync();
        }
    }

    /// <summary>
    /// A class for centralizing logic for accessing Azure storage, including reading the connection string and managing the account
    /// Child classes implement mapping of specific containers
    /// </summary>
    public abstract class StorageManager
    {
        #region Static Properties

        static string _connectionString = null;
        protected static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"];
                }

                return _connectionString;
            }
        }

        #endregion

        #region Properties

        CloudStorageAccount _account = null;
        protected CloudStorageAccount Account
        {
            get
            {
                if (_account == null)
                {
                    _account = CloudStorageAccount.Parse(ConnectionString);
                }

                return _account;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Enhances the given filename to make it unique
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GenerateUniqueName(string fileName)
        {
            Guid g = Guid.NewGuid();

            return string.Format("{0}-{1}", g.ToString("N"), fileName);
        }

        /// <summary>
        /// Builds the URL for the given accountName and container name
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="containerName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string GetContainerFileUrl(string containerName, string fileName)
        {
            string accountName = Account.Credentials.AccountName;
            return string.Format("//{0}.blob.core.windows.net/{1}/{2}", accountName, containerName, fileName);
        }

        protected CloudBlobContainer GetContainer(string name)
        {
            CloudBlobClient blobClient = Account.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = blobClient.GetContainerReference(name);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            return container;
        }

        #endregion
    }
}