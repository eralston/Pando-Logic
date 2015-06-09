using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Record for tracking a file uploaded to the cloud
    /// </summary>
    public class CloudFile : ModelBase
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
            file.CreatedDateUtc = DateTime.UtcNow;

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
        public static async Task DeleteAsync(this DbSet<CloudFile> files, CloudFile file, BlobStorageManagerBase storageManager)
        {
            if(file.ContainerName != null && file.BlobName != null)
            {
                var container = await storageManager.GetContainerAsync(file.ContainerName);
                await container.DeleteBlobAsync(file.BlobName);
            }
            
            files.Remove(file);
        }
    }
}