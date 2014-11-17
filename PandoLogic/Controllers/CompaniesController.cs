using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;

using PandoLogic.Models;

namespace PandoLogic.Controllers
{
    /// <summary>
    /// ViewModel for inviting someone into a team
    /// </summary>
    public class MemberInviteViewModel
    {
        public int CompanyId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Company Company { get; set; }
    }

    [NotMapped]
    public class CompanyViewModel : Company
    {
        const string FoundedDateFormat = "MM/dd/yyyy";

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Founded Date")]
        [DateTimeStringValidation(Format = FoundedDateFormat)]
        public string FoundedDateString { get; set; }

        public bool HasFoundedDate()
        {
            return !string.IsNullOrEmpty(FoundedDateString);
        }

        public DateTime ParsedDateTime()
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            return DateTime.ParseExact(FoundedDateString, FoundedDateFormat, provider);
        }
    }

    [Authorize]
    public class CompaniesController : BaseController
    {
        #region Methods

        private void ApplyTeamMembersToViewBag(Company company)
        {
            ViewBag.Members = Db.Members.WhereCompany(company);
            ViewBag.MemberInvites = Db.MemberInvites.WhereCompany(company.Id);
        }

        /// <summary>
        /// Asynchronously returns whether the current user is in the given company
        /// This should be used to double-check access rights for the user when accessing this controller
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public async Task<bool> IsCurrentUserInCompany(int companyId)
        {
            string currentUserId = this.UserCache.Id;
            var ret = await Db.Members.WhereAssignedToUserAndCompany(currentUserId, companyId);
            return ret != null;
        }

        #endregion

        // GET: Companies
        public async Task<ActionResult> Index()
        {
            var companies = Db.Companies.Include(c => c.Address).Include(c => c.Industry);
            return View(await companies.ToListAsync());
        }

        // GET: Companies/Details/5
        public async Task<ActionResult> Details(int id)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Company company = await Db.Companies.FindAsync(id);

            UnstashModelState();

            return View(company);
        }

        [ChildActionOnly]
        public ActionResult Team(int id)
        {
            Company company = Db.Companies.Find(id);
            if (company == null)
            {
                return HttpNotFound();
            }

            ApplyTeamMembersToViewBag(company);

            return View(company);
        }

        // GET: Companies/Create
        public ActionResult Create()
        {
            ViewBag.IndustryId = new SelectList(Db.Industries, "Id", "Title");
            CompanyViewModel viewModel = new CompanyViewModel();
            return View(viewModel);
        }

        // POST: Companies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Name,NumberOfEmployees,IndustryId,JobTitle,FoundedDateString")] CompanyViewModel companyViewModel)
        {
            if (ModelState.IsValid)
            {
                // Create a new company and capture viewModel fields
                Company company = new Company();
                await UpdateCompany(companyViewModel, company);

                // Add in implied data
                ApplicationUser currentUser = await GetCurrentUserAsync();
                company.Creator = currentUser;
                company.CreatedDate = DateTime.Now;

                // Creates a member to link to companies
                Member member = Db.Members.Create(currentUser, company);
                member.JobTitle = companyViewModel.JobTitle;
                Db.Companies.Add(company);

                // Save changes
                await Db.SaveChangesAsync();

                // Load the user cache
                await UpdateCurrentUserCacheAsync();

                // Continue to setting the company address
                return RedirectToAction("CreateCompany", "Addresses", new { id = company.Id });
            }

            ViewBag.IndustryId = new SelectList(Db.Industries, "Id", "Title", companyViewModel.IndustryId);
            return View(companyViewModel);
        }

        /// <summary>
        /// Updates the given company
        /// </summary>
        /// <param name="company"></param>
        /// <param name="origCompany"></param>
        /// <returns></returns>
        private async Task UpdateCompany(CompanyViewModel company, Company origCompany)
        {
            // Set properties
            origCompany.Name = company.Name;
            origCompany.NumberOfEmployees = company.NumberOfEmployees;
            origCompany.IndustryId = company.IndustryId;

            if (company.HasFoundedDate())
            {
                origCompany.FoundedDate = company.ParsedDateTime();
            }
            else
            {
                origCompany.FoundedDate = null;
            }


            // Check for avatar upload
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file.ContentLength > 0)
                {
                    // If we have one, then upload to Azure
                    string fileName = StorageManager.GenerateUniqueName(file.FileName);
                    await StorageManager.CompanyImages.UploadBlobAsync(fileName, file.InputStream);

                    // Set the URL
                    string fileUrl = StorageManager.GetCompanyImageUrl(fileName);
                    origCompany.AvatarUrl = fileUrl;
                    origCompany.AvatarFileName = file.FileName;
                }
            }
        }

        // GET: Companies/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Company company = await Db.Companies.FindAsync(id);
            if (company == null)
            {
                return HttpNotFound();
            }
            ViewBag.AddressId = new SelectList(Db.Addresses, "Id", "Address1", company.AddressId);
            ViewBag.IndustryId = new SelectList(Db.Industries, "Id", "Title", company.IndustryId);
            return View(company);
        }

        // POST: Companies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,CreatedDate,Name,NumberOfEmployees,IndustryId,AddressId")] Company company)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(company.Id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                Db.Entry(company).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.AddressId = new SelectList(Db.Addresses, "Id", "Address1", company.AddressId);
            ViewBag.IndustryId = new SelectList(Db.Industries, "Id", "Title", company.IndustryId);
            return View(company);
        }

        // GET: Companies/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Company company = await Db.Companies.FindAsync(id);
            if (company == null)
            {
                return HttpNotFound();
            }
            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Company company = await Db.Companies.FindAsync(id);
            Db.Companies.Remove(company);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Changes the user's current context to company with the given ID
        /// Then redirect to the URL at the "ReturnUrl" querystring
        /// NOTE: This will do nothing if the current user is NOT a member of that company
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Change(int id)
        {
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(id);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            // Double-check user is member of company
            ApplicationUser user = await GetCurrentUserAsync();
            Member[] memberships = await Db.Members.WhererUserIsMember(user).ToArrayAsync();

            // Pull out the URL to which we will redirect
            string url = Request.QueryString["returnurl"];
            if (string.IsNullOrEmpty(url))
            {
                url = Url.Action("Index", "Home");
            }

            // Try to find the membership for the desired company
            foreach (Member member in memberships)
            {
                if (member.CompanyId == id)
                {
                    member.SetSelected();
                    await Db.SaveChangesAsync();
                    await UpdateCurrentUserCacheAsync();
                    return Redirect(url);
                }
            }

            // If we didn't find anything, then let's just redirect without having done anything
            return Redirect(url);
        }

        #region Action Methods

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Invite(MemberInviteViewModel memberInvite)
        {
            // Double-check user has access
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(memberInvite.CompanyId);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            // If it's valid, then move along
            if (ModelState.IsValid)
            {
                // Create the invite and send an e-mail
                MemberInvite invite = Db.MemberInvites.Create(memberInvite.Email, memberInvite.CompanyId);
                ApplicationUser user = await GetCurrentUserAsync();
                await Db.SaveChangesAsync();

                // Send an invite e-mail
                EmailTemplates.SendInviteEmail(user, this, invite);
            }
            else
            {
                // Save the errors for the return
                StashModelState();
            }

            return RedirectToAction("Details", new { id = memberInvite.CompanyId });
        }

        public async Task<ActionResult> RevokeInvite(int id)
        {
            // Pull the invite
            MemberInvite invite = await Db.MemberInvites.FindAsync(id);

            if (invite == null)
                return HttpNotFound();

            // Verify current user is in this team
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(invite.CompanyId);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Company company = await Db.Companies.FindAsync(invite.CompanyId);
            ViewBag.MemberInviteId = id;
            ViewBag.MemberInviteEmail = invite.Email ?? "";

            return View(company);
        }

        [HttpPost, ActionName("RevokeInvite")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RevokeInviteConfirmed(int id)
        {
            // Pull the invite
            MemberInvite invite = await Db.MemberInvites.FindAsync(id);

            if (invite == null)
                return HttpNotFound();

            // Verify current user is in this team
            bool isCurrentUserAllowed = await IsCurrentUserInCompany(invite.CompanyId);
            if (!isCurrentUserAllowed)
                return HttpNotFound();

            Db.MemberInvites.Remove(invite);
            await Db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = invite.CompanyId });
        }

        #endregion
    }
}
