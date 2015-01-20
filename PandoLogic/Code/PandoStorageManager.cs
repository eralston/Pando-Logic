using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace PandoLogic
{
    /// <summary>
    /// Storage manager for Pando Logic containers
    /// </summary>
    public class PandoStorageManager : StorageManager
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
        public CloudBlobContainer UserImages
        {
            get
            {
                if (_userImageContainer == null)
                    _userImageContainer = GetContainer(UserImageContainerName);

                return _userImageContainer;
            }
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
        public CloudBlobContainer CompanyImages
        {
            get
            {
                if (_companyImageContainer == null)
                    _companyImageContainer = GetContainer(CompanyImageContainerName);

                return _companyImageContainer;
            }
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