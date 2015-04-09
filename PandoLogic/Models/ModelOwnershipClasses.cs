using System;
using System.Web;

using Masticore;

namespace PandoLogic.Models
{
    /// <summary>
    /// Int3erface for any object that is owned by a single user
    /// </summary>
    public interface IUserOwnedModel
    {
        string UserId { get; set; }
    }

    /// <summary>
    /// Interface for an object that is optionally owned by a company (IE can be null)
    /// </summary>
    public interface IOptionalCompanyOwnedModel : IBaseModel
    {
        int? CompanyId { get; set; }
        Company Company { get; set; }
    }

    /// <summary>
    /// Interface for an object that is required to be owned by a company
    /// </summary>
    public interface IRequiredCompanyOwnedModel : IBaseModel
    {
        int CompanyId { get; set; }
        Company Company { get; set; }
    }

    /// <summary>
    /// A class for extensions on the ownership interfaces outlined above
    /// </summary>
    public static class OwnershipExtensions
    {
        /// <summary>
        /// Returns true if the given model matches the user's ID; otherwise, returns null
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool IsOwnedByUser(this IUserOwnedModel model, string userId)
        {
            return model.UserId == userId;
        }

        /// <summary>
        /// Throws an exception if the given model does not match the given userId
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        public static void ExceptIfNotOwnedByUser(this IUserOwnedModel model, string userId)
        {
            if (model.IsOwnedByUser(userId))
                return;

            throw new HttpException(404, "Not Found");
        }

        /// <summary>
        /// Returns true if the given model has a matching company ID; else false
        /// If the model's ID is null, then it returns the value passed for defaultOnNull
        /// </summary>
        /// <param name="model"></param>
        /// <param name="companyId"></param>
        /// <param name="defaultOnNull">Value returned if the model's optional ID is null; default value is false</param>
        /// <returns></returns>
        public static bool IsOwnedByCompany(this IOptionalCompanyOwnedModel model, int companyId, bool defaultOnNull = false)
        {
            if (model.CompanyId.HasValue)
                return model.CompanyId == companyId;
            else
                return defaultOnNull;
        }

        /// <summary>
        /// Throws an exception if the given model does not match the given companyId
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        public static void ExceptIfNotOwnedByCompany(this IOptionalCompanyOwnedModel model, int companyId, bool defaultOnNull = false)
        {
            if (model.IsOwnedByCompany(companyId, defaultOnNull))
                return;

            throw new HttpException(404, "Not Found");
        }

        /// <summary>
        /// Returns true if the given model has a matching company ID; else, false
        /// </summary>
        /// <param name="model"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public static bool IsOwnedByCompany(this IRequiredCompanyOwnedModel model, int companyId)
        {
            return model.CompanyId == companyId;
        }

        /// <summary>
        /// Throws an exception if the given model does not match the given userId
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        public static void ExceptIfNotOwnedByCompany(this IRequiredCompanyOwnedModel model, int companyId)
        {
            if (model.IsOwnedByCompany(companyId))
                return;

            throw new HttpException(404, "Not Found");
        }
    }
}
