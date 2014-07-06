using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PandoLogic.Models;

namespace PandoLogic.Controllers
{
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
            return View();
        }

        // POST: Companies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Name,NumberOfEmployees,IndustryId")] Company company)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser currentUser = await GetCurrentUserAsync();
                company.Creator = currentUser;
                company.CreatedDate = DateTime.Now;
                Member member = Db.Members.Create(currentUser, company);
                Db.Companies.Add(company);
                await Db.SaveChangesAsync();
                return RedirectToAction("CreateCompany", "Addresses", new { id = company.Id });
            }

            ViewBag.IndustryId = new SelectList(Db.Industries, "Id", "Title", company.IndustryId);
            return View(company);
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
