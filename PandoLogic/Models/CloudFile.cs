using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;

namespace PandoLogic.Models
{
    /// <summary>
    /// Record for tracking a file uploaded to the cloud
    /// </summary>
    public class CloudFile : BaseModel
    {
        public string Url { get; set; }

        public string DisplayName { get; set; }

        public string ContainerName { get; set; }

        public string BlobName { get; set; }
    }

    /// <summary>
    /// Extensions pertaining to the CloudFile class, including interaction with file storage and the database
    /// </summary>
    public static class CloudFileExtensions
    {
        /// <summary>
        /// Creates and adds a new CloudFile record for the given fields to the system
        /// </summary>
        /// <param name="files"></param>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="url"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static CloudFile Create(this DbSet<CloudFile> files, string containerName, string blobName, string url, string displayName = "")
        {
            CloudFile file = new CloudFile();
            file.CreatedDate = DateTime.Now;

            file.Url = url;
            file.DisplayName = displayName;
            file.BlobName = blobName;
            file.ContainerName = containerName;

            files.Add(file);

            return file;
        }

        /// <summary>
        /// Deletes the given files from the bucket and the database
        /// </summary>
        /// <param name="files"></param>
        /// <param name="file"></param>
        /// <param name="storageManager"></param>
        /// <returns></returns>
        public static Task DeleteAsync(this DbSet<CloudFile> files, CloudFile file, StorageManager storageManager)
        {
            Task task = null;

            if(file.ContainerName != null && file.BlobName != null)
            {
                var container = storageManager.GetContainer(file.ContainerName);
                task = container.DeleteBlobAsync(file.BlobName);
            }
            
            files.Remove(file);

            return task;
        }
    }
}