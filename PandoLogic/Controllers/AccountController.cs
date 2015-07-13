using InviteOnly;
using Masticore;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.WindowsAzure.Storage.Blob;
using Owin;
using PandoLogic.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using StripeEntities;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// ViewModel for the payment page, carrying the payment information
    /// https://stripe.com/docs/stripe.js/switching
    /// </summary>
    public class PaymentViewModel
    {
        [Required]
        public string stripeToken { get; set; }
    }

    public class SubscriptionViewModel
    {
        public SubscriptionViewModel(Plan[] plans, Company company)
        {
            this.Plans = plans;
            this.Company = company;
        }

        public Plan[] Plans { get; set; }

        public Company Company { get; set; }
    }

    public class SubscriptionPostViewModel
    {
        public int PlanId { get; set; }
        public int CompanyId { get; set; }
    }

    [Authorize]
    public class AccountController : BaseController
    {
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindAsync(model.Email, model.Password);
                if (user != null)
                {
                    await SignInAsync(user, model.RememberMe);

                    ClearCurrentUserCache();

                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    System.Diagnostics.Trace.TraceWarning("Invalid username and password for user '{0}'", model.Email);
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }

            IdentityResult result = await UserManager.ConfirmEmailAsync(userId, code);
            if (result.Succeeded)
            {
                return View("ConfirmEmail");
            }
            else
            {
                AddErrors(result);
                return View();
            }
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            System.Diagnostics.Trace.TraceInformation("Forget password attempt for user '{0}'", model.Email);
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Sorry, the user could not be found.");
                    return View();
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "BizSprout Password Reset", "Please reset your BizSprout password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                EmailTemplates.SendPasswordResetEmail(user, this, callbackUrl);
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return View("Error");
            }
            return View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            System.Diagnostics.Trace.TraceInformation("Reset password attempt for user '{0}'", model.Email);

            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No user found.");
                    return View();
                }
                IdentityResult result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                else
                {
                    AddErrors(result);
                    return View();
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                await SignInAsync(user, isPersistent: false);
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public async Task<ActionResult> Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            ApplicationUser user = await GetCurrentUserAsync();
            ViewBag.CurrentApplicationUser = user;

            // Merge in any errors from redirecting actions
            UnstashModelState();

            // Find the list of companies the current user is a member of
            await LoadCompaniesForCurrentUserIntoViewBag();
            await LoadSubscriptionsForCurrentUserIntoViewBag();

            if (user.HasPaymentInfo)
            {
                Stripe.StripeCustomer stripeCustomer = StripeManager.RetrieveCustomer(user);
                ViewBag.DefaultCard = stripeCustomer.GetDefaultCard();
            }

            return View();
        }

        private async Task LoadSubscriptionsForCurrentUserIntoViewBag()
        {
            ViewBag.Subscriptions = await Db.Subscriptions.WhereUser(UserCache.Id).Include(s => s.Plan).Include(s => s.Company).ToArrayAsync();
        }

        private async Task LoadCompaniesForCurrentUserIntoViewBag()
        {
            ApplicationUser currentUser = await GetCurrentUserAsync();
            ViewBag.Companies = await Db.CompaniesWhereUserIsMember(currentUser.Id).ToArrayAsync();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");


            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                        await SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            ApplicationUser currentUser = await GetCurrentUserAsync();
            ViewBag.CurrentApplicationUser = currentUser;

            // Find the list of companies the current user is a member of
            await LoadCompaniesForCurrentUserIntoViewBag();
            await LoadSubscriptionsForCurrentUserIntoViewBag();

            Stripe.StripeCustomer stripeCustomer = StripeManager.RetrieveCustomer(currentUser);
            ViewBag.DefaultCard = stripeCustomer.GetDefaultCard();

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
        }

        #region Payments

        private async Task UpdatePaymentInfo(PaymentViewModel viewModel)
        {
            ApplicationUser currentUser = await GetCurrentUserAsync();

            // Create the customer in stripe with this payment information
            StripeManager.CreateOrUpdateCustomer(currentUser, viewModel.stripeToken);

            await Db.SaveChangesAsync();
        }

        /// <summary>
        /// GET for payment page, enabling user to enter card information
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Payment()
        {
            // If the current user has payment info, then we can skip this, unless they are editing it
            ApplicationUser currentUser = await GetCurrentUserAsync();

            ViewBag.PostTarget = "Payment";

            if (currentUser.HasPaymentInfo)
                return RedirectToAction("Subscription");
            else
                return View();
        }

        /// <summary>
        /// POST for 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Payment(PaymentViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    await UpdatePaymentInfo(viewModel);

                    return RedirectToAction("Subscription");
                }

                ViewBag.PostTarget = "Payment";
                return View();
            }
            catch (Stripe.StripeException ex)
            {
                // In the event of stripe exception, assume it's the user's problem
                ModelState.AddModelError("Custom", ex);
                return View();
            }
        }

        /// <summary>
        /// GET for payment page, enabling user to enter card information
        /// </summary>
        /// <returns></returns>
        public ActionResult PaymentChange()
        {
            ViewBag.PostTarget = "PaymentChange";
            return View("Payment");
        }

        /// <summary>
        /// POST for 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PaymentChange(PaymentViewModel viewModel)
        {

            if (ModelState.IsValid)
            {
                await UpdatePaymentInfo(viewModel);

                return RedirectToAction("Manage");
            }

            ViewBag.PostTarget = "PaymentChange";
            return View("Payment");
        }

        #endregion

        /// <summary>
        /// GET request for the page where the user selects subscription level
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> Subscription()
        {
            Plan[] plans = await Db.Plans.AllAvailablePlans().ToArrayAsync();

            // If we are entering this page, then double-check consistency of member state
            await UpdateCurrentUserCacheAsync();
            Member member = await GetCurrentMemberAsync();

            // If we don't have a member, then we need to start with making a company
            if (member == null)
            {
                System.Diagnostics.Trace.TraceError("User {0} tried to access subscription without a company", GetCurrentUser().Email);
                return RedirectToAction("Create", "Companies");
            }

            Subscription companySubscription = await Db.Subscriptions.WhereCompany(member.CompanyId);

            // If the current company has a subscription, but it's not owned by the current user
            if (companySubscription != null && companySubscription.UserId != UserCache.Id)
            {
                return RedirectToAction("Details", "Companies", new { id = member.CompanyId });
            }

            // If the current company has a subscription and it's owned by the current user, then indicate with the from
            if (companySubscription != null)
            {
                ViewBag.HasExistingSubscription = true;
            }

            SubscriptionViewModel viewModel = new SubscriptionViewModel(plans, member.Company);

            return View(viewModel);
        }

        /// <summary>
        /// POST request for confirming user's subscription level
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscription(SubscriptionPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Context for this subscription change
                ApplicationUser currentUser = await GetCurrentUserAsync();
                Member member = await GetCurrentMemberAsync();

                // Make sure Stripe has what it needs on the user
                StripeManager.CreateOrUpdateCustomer(currentUser);

                // If there is an existing subscription, then unsubscibe and delete it
                Plan desiredPlan = await Db.Plans.FindAsync(viewModel.PlanId);
                Subscription oldSubscription = await Db.Subscriptions.WhereUserAndCompany(currentUser.Id, member.CompanyId);
                if (oldSubscription != null && oldSubscription.PaymentSystemId != null)
                {
                    // Transfer this user to the new plan
                    oldSubscription.Plan = desiredPlan;
                    StripeManager.ChangeSubscriptionPlan(currentUser, oldSubscription, desiredPlan);
                }
                else
                {
                    // If we have a leftover dead subscription, then kill it
                    if (oldSubscription != null)
                        Db.Subscriptions.Remove(oldSubscription);

                    // Create new subscription based on selected plan
                    Subscription newSubscription = Db.Subscriptions.Create(currentUser, member.Company, desiredPlan);
                    StripeManager.Subscribe(currentUser, newSubscription, desiredPlan);
                }

                // Save all changes
                await Db.SaveChangesAsync();

                // Send them home
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
                IdentityResult result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInAsync(user, isPersistent: false);

                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        // SendEmail(user.Email, callbackUrl, "Confirm your account", "Please confirm your account by clicking this link");

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        public ActionResult LogOff()
        {
            ClearCurrentUserCache();
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }

        #region Account Register

        /// <summary>
        /// A public view that allows an unregistered user to pick their subscription plan
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<ActionResult> SubscriptionPlan()
        {
            var plans = await Db.Plans.AllAvailablePlans().OrderByDescending(p => p.Price).ToArrayAsync();

            return View(plans);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            string productId = Request.QueryString["ProductId"];
            string subscriptionPlanId = Request.QueryString["SubscriptPlanId"];

            if (string.IsNullOrEmpty(productId) && string.IsNullOrEmpty(subscriptionPlanId))
            {
                return RedirectToAction("SubscriptionPlan");
            }

            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName, CreatedDate = DateTime.UtcNow };
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInAsync(user, isPersistent: false);

                    user.VerificationInvite = Invite.Create(Db);
                    await Db.SaveChangesAsync();

                    EmailTemplates.SendWelcomeEmail(user, this);
                    EmailTemplates.SendNewUserNotification(user, this);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Create");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public async Task<ActionResult> Create()
        {
            ApplicationUser origUser = await GetCurrentUserAsync();
            return View(origUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                // Update other user fields and send onward ot create companies
                await UpdateCurrentUser(user);
                return RedirectToAction("Create", "Companies");
            }

            // If we got this far, something failed, redisplay form
            return View(user);
        }

        /// <summary>
        /// Performs the update for the current user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="origUser"></param>
        /// <returns></returns>
        private async Task UpdateCurrentUser(ApplicationUser user)
        {
            ApplicationUser origUser = await GetCurrentUserAsync();

            // Check for avatar upload
            if (Request.Files.Count > 0)
            {
                // Save it to Azure
                var file = Request.Files[0];

                if (file.ContentLength > 0)
                {
                    string blobName = BlobStorageManagerBase.GenerateUniqueName(file.FileName);

                    CloudBlobContainer container = await StorageManager.GetUserImagesAsync();
                    await container.UploadBlobAsync(blobName, file.InputStream);
                    string fileUrl = StorageManager.GetUserImageUrl(blobName);

                    CloudFile newFile = Db.CloudFiles.Create(PandoStorageManager.UserImageContainerName, blobName, fileUrl, file.FileName);

                    if (origUser.Avatar != null)
                        await Db.CloudFiles.DeleteAsync(origUser.Avatar, StorageManager);

                    origUser.Avatar = newFile;
                }
            }

            // Set properties
            origUser.FirstName = user.FirstName;
            origUser.LastName = user.LastName;

            // Save changes
            await Db.SaveChangesAsync();

            // Clear the cache
            await UpdateCurrentUserCacheAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                await UpdateCurrentUser(user);
                return RedirectToAction("Manage");
            }

            StashModelState();

            // If we got this far, something failed, redisplay form
            return RedirectToAction("Manage");
        }

        #endregion

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private void SendEmail(string email, string callbackUrl, string subject, string message)
        {
            // For information on sending mail, please visit http://go.microsoft.com/fwlink/?LinkID=320771
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}