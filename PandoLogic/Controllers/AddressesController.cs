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
    /// <summary>
    /// View model for handling address create and edit, that adds another field for the parent entity ID
    /// </summary>
    public class AddressViewModel : Address
    {
        public int ParentEntityId { get; set; }
    }

    public class AddressesController : BaseController
    {
        // GET: Addresses
        public async Task<ActionResult> Index()
        {
            return View(await Db.Addresses.ToListAsync());
        }

        // GET: Addresses/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Address address = await Db.Addresses.FindAsync(id);
            if (address == null)
            {
                return HttpNotFound();
            }
            return View(address);
        }

        // GET: Addresses/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Addresses/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,CreatedDate,LastUpdate,Address1,Address2,City,StateOrProvince,Country,PostalCode,Type")] Address address)
        {
            if (ModelState.IsValid)
            {
                Db.Addresses.Add(address);
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(address);
        }

        // GET: Addresses/CreateCompany
        public ActionResult CreateCompany(int id)
        {
            // TODO: LINQ query to make sure the person changing this company is allowed to do it
            AddressViewModel address = new AddressViewModel();
            address.ParentEntityId = id;
            return View(address);
        }

        // POST: Addresses/CreateCompany
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCompany([Bind(Include = "ParentEntityId,Address1,Address2,City,StateOrProvince,Country,PostalCode,Type")] AddressViewModel address)
        {
            // TODO: Double-check parent entity is valid for the current user to change
            if (ModelState.IsValid)
            {
                Address add = new Address();
                add.CreatedDate = DateTime.Now;
                add.LastUpdate = DateTime.Now;

                add.Address1 = address.Address1;
                add.Address2 = address.Address2;
                add.City = address.City;
                add.Country = address.Country;
                add.PostalCode = address.PostalCode;
                add.StateOrProvince = address.StateOrProvince;
                add.Type = address.Type;

                Company company = await Db.Companies.FindAsync(address.ParentEntityId);
                company.Address = add;

                Db.Addresses.Add(add);
                await Db.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }

            return View(address);
        }

        // GET: Addresses/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Address address = await Db.Addresses.FindAsync(id);
            if (address == null)
            {
                return HttpNotFound();
            }
            return View(address);
        }

        // POST: Addresses/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,CreatedDate,LastUpdate,Address1,Address2,City,StateOrProvince,Country,PostalCode,Type")] Address address)
        {
            if (ModelState.IsValid)
            {
                Db.Entry(address).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(address);
        }

        // GET: Addresses/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Address address = await Db.Addresses.FindAsync(id);
            if (address == null)
            {
                return HttpNotFound();
            }
            return View(address);
        }

        // POST: Addresses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Address address = await Db.Addresses.FindAsync(id);
            Db.Addresses.Remove(address);
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
