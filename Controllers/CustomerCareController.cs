using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;
using TestingDemo.ViewModels;

namespace TestingDemo.Controllers
{
    [Authorize]
    public class CustomerCareController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public CustomerCareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CustomerCare/Index
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? liaisonPageNumber, int? receivedPageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            int pageSize = 5; // Smaller page size for two tables

            // Query for Liaison Clients
            var liaisonQuery = _context.Clients
                .Where(c => c.Status == "Liaison")
                .AsNoTracking();

            // Query for Received Clients
            var receivedQuery = _context.Clients
                .Where(c => c.Status == "CustomerCareReceived")
                .AsNoTracking();

            // Apply searching
            if (!String.IsNullOrEmpty(searchString))
            {
                liaisonQuery = liaisonQuery.Where(s => s.ClientName.Contains(searchString) || s.PermitType.Contains(searchString));
                receivedQuery = receivedQuery.Where(s => s.ClientName.Contains(searchString) || s.PermitType.Contains(searchString));
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "name_desc":
                    liaisonQuery = liaisonQuery.OrderByDescending(s => s.ClientName);
                    receivedQuery = receivedQuery.OrderByDescending(s => s.ClientName);
                    break;
                case "Date":
                    liaisonQuery = liaisonQuery.OrderBy(s => s.CreatedDate);
                    receivedQuery = receivedQuery.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    liaisonQuery = liaisonQuery.OrderByDescending(s => s.CreatedDate);
                    receivedQuery = receivedQuery.OrderByDescending(s => s.CreatedDate);
                    break;
                default:
                    liaisonQuery = liaisonQuery.OrderBy(s => s.CreatedDate);
                    receivedQuery = receivedQuery.OrderBy(s => s.CreatedDate);
                    break;
            }

            var viewModel = new CustomerCareDashboardViewModel
            {
                LiaisonClients = await PaginatedList<ClientModel>.CreateAsync(liaisonQuery, liaisonPageNumber ?? 1, pageSize),
                ReceivedClients = await PaginatedList<ClientModel>.CreateAsync(receivedQuery, receivedPageNumber ?? 1, pageSize)
            };

            return View(viewModel);
        }

        // GET: CustomerCare/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();
            var requirements = await _context.PermitRequirements.Where(r => r.ClientId == id).ToListAsync();
            ViewBag.Requirements = requirements;
            return View(client);
        }

        // POST: CustomerCare/MarkAsReceived/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReceived(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "CustomerCareReceived";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client marked as received.";
            }
            return RedirectToAction("Index");
        }

        // POST: CustomerCare/ProceedToDocumentOfficer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToDocumentOfficer(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "DocumentOfficer";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client moved to Document Officer.";
            }
            return RedirectToAction("Index");
        }

        // POST: CustomerCare/ReturnToPlanning/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToPlanning(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "Planning";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client returned to Planning Officer.";
            }
            return RedirectToAction("Index");
        }

        // POST: CustomerCare/SaveRequirements/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRequirements(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            var requirements = await _context.PermitRequirements.Where(r => r.ClientId == id).ToListAsync();

            foreach (var requirement in requirements)
            {
                var isPresentKey = $"present_{requirement.Id}";
                requirement.IsPresent = Request.Form.ContainsKey(isPresentKey);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Requirements inspection saved successfully.";

            return RedirectToAction("Index");
        }
    }
}