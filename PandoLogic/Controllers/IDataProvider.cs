using System;
namespace PandoLogic.Controllers
{
    /// <summary>
    /// Public interface for something that provides data access
    /// </summary>
    public interface IDataProvider
    {
        PandoLogic.Models.ApplicationDbContext Db { get; }
        PandoLogic.PandoStorageManager StorageManager { get; }
    }
}
