using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;
using TestingDemo.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace TestingDemo.Controllers
{
    [Authorize(Roles = "CustomerCare,Admin")]
    public class CustomerCareController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CustomerCareController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                liaisonQuery = liaisonQuery.Where(s => s.ClientName.Contains(searchString) || s.TypeOfProject.Contains(searchString));
                receivedQuery = receivedQuery.Where(s => s.ClientName.Contains(searchString) || s.TypeOfProject.Contains(searchString));
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
            var client = await _context.Clients
                .Include(c => c.RetainershipBIR)
                .Include(c => c.RetainershipSPP)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .FirstOrDefaultAsync(c => c.Id == id);
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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "CustomerCare data changed");
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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "CustomerCare data changed");
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

        [HttpGet]
        public async Task<IActionResult> GetLatestData(string sortOrder, string searchString, int? liaisonPageNumber, int? receivedPageNumber)
        {
            int pageSize = 5;
            var liaisonQuery = _context.Clients.Where(c => c.Status == "Liaison").AsNoTracking();
            var receivedQuery = _context.Clients.Where(c => c.Status == "CustomerCareReceived").AsNoTracking();
            if (!string.IsNullOrEmpty(searchString))
            {
                liaisonQuery = liaisonQuery.Where(s => s.ClientName.Contains(searchString) || s.TypeOfProject.Contains(searchString));
                receivedQuery = receivedQuery.Where(s => s.ClientName.Contains(searchString) || s.TypeOfProject.Contains(searchString));
            }
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
            var viewModel = new TestingDemo.ViewModels.CustomerCareDashboardViewModel
            {
                LiaisonClients = await TestingDemo.Models.PaginatedList<TestingDemo.Models.ClientModel>.CreateAsync(liaisonQuery, liaisonPageNumber ?? 1, pageSize),
                ReceivedClients = await TestingDemo.Models.PaginatedList<TestingDemo.Models.ClientModel>.CreateAsync(receivedQuery, receivedPageNumber ?? 1, pageSize)
            };
            return Json(viewModel);
        }
    }
}