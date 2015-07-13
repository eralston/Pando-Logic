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

using Masticore;

namespace PandoLogic.Controllers
{
    public class OrderDocumentViewModel
    {
        public string Title { get; private set; }
        public string SubTitle { get; set; }
    }

    /// <summary>
    /// Controller for administration of the available services in the system, plus:
    /// End-user ability to order them (Create service requests)
    /// Back office process for fulfilling them (Processing service requests)
    /// </summary>
    [Authorize]
    public class ServicesController : BaseController
    {
        // GET: Services
        public async Task<ActionResult> Index()
        {
            Service[] services = await Db.Services.WhereActiveAndPublic().ToArrayAsync();
            return View(services);
        }

        /// <summary>
        /// Retrieves the order form for a specific service
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> Order(int id)
        {
            Service service = await Db.Services.FindAsync(id);
            ViewBag.Service = service;
            return View();
        }

        /// <summary>
        /// Subits the form for the post form
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Order")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostOrder(int id)
        {
            return RedirectToAction("Index");
        }

        [AdminAuthorize]
        public async Task<ActionResult> List()
        {
            return View(await Db.Services.ToListAsync());
        }

        [AdminAuthorize]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = await Db.Services.FindAsync(id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // GET: Services/Create
        [AdminAuthorize]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Services/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public async Task<ActionResult> Create(Service service)
        {
            if (ModelState.IsValid)
            {
                Service newService = Db.Services.Create(service.Title);
                newService.MergeEditableProperties(service);

                await Db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(service);
        }

        // GET: Services/Edit/5
        [AdminAuthorize]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = await Db.Services.FindAsync(id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // POST: Services/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public async Task<ActionResult> Edit(Service service)
        {
            if (ModelState.IsValid)
            {
                Service existingService = await Db.Services.FindAsync(service.Id);
                existingService.MergeEditableProperties(service);
                await Db.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            return View(service);
        }

        // GET: Services/Delete/5
        [AdminAuthorize]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = await Db.Services.FindAsync(id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Service service = await Db.Services.FindAsync(id);
            service.State = StripeEntities.Product.ProductState.Retired;
            await Db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        #region Methods for Specific Services

        /// <summary>
        /// Retrieves the order form for a service around document handling
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> OrderDocument(int id)
        {
            Service service = await Db.Services.FindAsync(id);
            ViewBag.Service = service;
            return View();
        }

        /// <summary>
        /// Submits the order for a document service
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("OrderDocument")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostOrderDocument(ServiceRequest request)
        {
            if (ModelState.IsValid)
            {

            }
            return View(request);
        }

        #endregion
    }
}
