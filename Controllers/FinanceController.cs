using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;
using TestingDemo.ViewModels;

namespace TestingDemo.Controllers
{
    [Authorize] // Requires authentication
    public class FinanceController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public FinanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Finance/Index
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pendingPageNumber, int? clearancePageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            int pageSize = 5;

            var pendingQuery = _context.Clients.Where(c => c.Status == "Pending" || c.Status == "Finance").AsNoTracking();
            var clearanceQuery = _context.Clients.Where(c => c.Status == "Clearance").AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {
                pendingQuery = pendingQuery.Where(s => s.ClientName.Contains(searchString) || s.PermitType.Contains(searchString));
                clearanceQuery = clearanceQuery.Where(s => s.ClientName.Contains(searchString) || s.PermitType.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    pendingQuery = pendingQuery.OrderByDescending(s => s.ClientName);
                    clearanceQuery = clearanceQuery.OrderByDescending(s => s.ClientName);
                    break;
                case "Date":
                    pendingQuery = pendingQuery.OrderBy(s => s.CreatedDate);
                    clearanceQuery = clearanceQuery.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    pendingQuery = pendingQuery.OrderByDescending(s => s.CreatedDate);
                    clearanceQuery = clearanceQuery.OrderByDescending(s => s.CreatedDate);
                    break;
                default:
                    pendingQuery = pendingQuery.OrderBy(s => s.CreatedDate);
                    clearanceQuery = clearanceQuery.OrderBy(s => s.CreatedDate);
                    break;
            }

            var viewModel = new FinanceDashboardViewModel
            {
                PendingClients = await PaginatedList<ClientModel>.CreateAsync(pendingQuery, pendingPageNumber ?? 1, pageSize),
                ClearanceClients = await PaginatedList<ClientModel>.CreateAsync(clearanceQuery, clearancePageNumber ?? 1, pageSize)
            };

            return View(viewModel);
        }

        // GET: Finance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Finance/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Finance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientModel client)
        {
            // Clear validation errors for nullable fields if they're empty
            if (string.IsNullOrEmpty(client.TaxId))
                ModelState.Remove("TaxId");

            if (ModelState.IsValid)
            {
                client.CreatedDate = DateTime.Now;
                client.Status = "Pending";

                _context.Add(client);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Client created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Finance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Finance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClientModel client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            // Clear validation errors for nullable fields if they're empty
            if (string.IsNullOrEmpty(client.TaxId))
                ModelState.Remove("TaxId");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Client updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Finance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Finance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Finance/SendToPlanningOfficer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToPlanningOfficer(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client != null)
            {
                // Update status to indicate it's ready for planning
                client.Status = "Planning";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Client sent to Planning Officer successfully!";

                // Redirect to Planning Officer controller if user has permission
                if (User.IsInRole("PlanningOfficer") || User.IsInRole("Admin"))
                {
                    return RedirectToAction("PlanRequirements", "PlanningOfficer", new { id = client.Id });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Finance/ArchiveClient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "Archived";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client {client.ClientName} has been archived successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}