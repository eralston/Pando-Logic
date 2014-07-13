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

    public class CompaniesController : BaseController
    {
        // GET: Companies
        public async Task<ActionResult> Index()
        {
            var companies = Db.Companies.Include(c => c.Address).Include(c => c.Industry);
            return View(await companies.ToListAsync());
        }

        // GET: Companies/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Company company = await Db.Companies.FindAsync(id);
            if (company == null)
            {
                return HttpNotFound();
            }
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
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
            Company company = await Db.Companies.FindAsync(id);
            Db.Companies.Remove(company);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
