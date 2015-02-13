using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PandoLogic
{
    /// <summary>
    /// Storage manager for 
    /// </summary>
    public class PandoStorageManager : BlobStorageManager
    {
        #region Constants

        public const string UserImageContainerName = "userimages";
        public const string CompanyImageContainerName = "companyimages";

        #endregion

        #region Properties

        CloudBlobContainer _userImageContainer = null;
        /// <summary>
        /// Blob container for reports in the system
        /// </summary>
        public async Task<CloudBlobContainer> GetUserImagesAsync()
        {
            if (_userImageContainer == null)
                _userImageContainer = await GetContainerAsync(UserImageContainerName);

            return _userImageContainer;
        }

        /// <summary>
        /// Gets the URL for the given filename in the user container
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetUserImageUrl(string fileName)
        {
            return this.GetContainerFileUrl(UserImageContainerName, fileName);
        }

        CloudBlobContainer _companyImageContainer = null;
        /// <summary>
        /// Blob container for reports in the system
        /// </summary>
        public async Task<CloudBlobContainer> GetCompanyImagesAsync()
        {
            if (_companyImageContainer == null)
                _companyImageContainer = await GetContainerAsync(CompanyImageContainerName);

            return _companyImageContainer;
        }

        /// <summary>
        /// Gets the URL for the given filename in the company container
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetCompanyImageUrl(string fileName)
        {
            return this.GetContainerFileUrl(CompanyImageContainerName, fileName);
        }

        #endregion
    }
}