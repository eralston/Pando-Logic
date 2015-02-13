using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace PandoLogic
{
    /// <summary>
    /// A base class for managing azure storage (blob or table)
    /// This implements controlling the connection string and loading it from the app settings
    /// </summary>
    public abstract class BaseStorageManager
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
    }
}