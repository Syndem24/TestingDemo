using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;
using TestingDemo.Data;

namespace TestingDemo.Controllers
{
    [Authorize(Roles = "Admin,DocumentOfficer")]
    public class ArchiveController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ArchiveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Archive/Index
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber, string TypeOfProject, string CreatedDateFrom, string CreatedDateTo)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["TypeSortParm"] = sortOrder == "Type" ? "type_desc" : "Type";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var clients = from c in _context.Clients
                          where c.Status == "Archived"
                          select c;

            if (!String.IsNullOrEmpty(searchString))
            {
                clients = clients.Where(s => s.ClientName.Contains(searchString));
            }
            if (!String.IsNullOrEmpty(TypeOfProject))
            {
                clients = clients.Where(s => s.TypeOfProject == TypeOfProject);
            }
            if (!String.IsNullOrEmpty(CreatedDateFrom) && DateTime.TryParse(CreatedDateFrom, out var createdDateFrom))
            {
                clients = clients.Where(s => s.CreatedDate.Date >= createdDateFrom.Date);
            }
            if (!String.IsNullOrEmpty(CreatedDateTo) && DateTime.TryParse(CreatedDateTo, out var createdDateTo))
            {
                clients = clients.Where(s => s.CreatedDate.Date <= createdDateTo.Date);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.ClientName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.CreatedDate);
                    break;
                case "Type":
                    clients = clients.OrderBy(s => s.TypeOfProject);
                    break;
                case "type_desc":
                    clients = clients.OrderByDescending(s => s.TypeOfProject);
                    break;
                case "Status":
                    clients = clients.OrderBy(s => s.Status);
                    break;
                case "status_desc":
                    clients = clients.OrderByDescending(s => s.Status);
                    break;
                default:
                    clients = clients.OrderBy(s => s.CreatedDate);
                    break;
            }

            int pageSize = 10;
            return View(await PaginatedList<ClientModel>.CreateAsync(clients.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Archive/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.RetainershipSPP)
                .Include(c => c.RetainershipBIR)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            ViewBag.Requirements = await _context.PermitRequirements
                .Where(r => r.ClientId == id)
                .OrderBy(r => r.CreatedDate)
                .ToListAsync();

            return View(client);
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestData(string sortOrder, string currentFilter, string searchString, int? pageNumber, string TypeOfProject, string CreatedDateFrom, string CreatedDateTo)
        {
            var clients = from c in _context.Clients
                          where c.Status == "Archived"
                          select c;
            if (!string.IsNullOrEmpty(searchString))
                clients = clients.Where(s => s.ClientName.Contains(searchString));
            if (!string.IsNullOrEmpty(TypeOfProject))
                clients = clients.Where(s => s.TypeOfProject == TypeOfProject);
            if (!string.IsNullOrEmpty(CreatedDateFrom) && DateTime.TryParse(CreatedDateFrom, out var createdDateFrom))
                clients = clients.Where(s => s.CreatedDate.Date >= createdDateFrom.Date);
            if (!string.IsNullOrEmpty(CreatedDateTo) && DateTime.TryParse(CreatedDateTo, out var createdDateTo))
                clients = clients.Where(s => s.CreatedDate.Date <= createdDateTo.Date);
            switch (sortOrder)
            {
                case "name_desc":
                    clients = clients.OrderByDescending(s => s.ClientName);
                    break;
                case "Date":
                    clients = clients.OrderBy(s => s.CreatedDate);
                    break;
                case "date_desc":
                    clients = clients.OrderByDescending(s => s.CreatedDate);
                    break;
                case "Type":
                    clients = clients.OrderBy(s => s.TypeOfProject);
                    break;
                case "type_desc":
                    clients = clients.OrderByDescending(s => s.TypeOfProject);
                    break;
                case "Status":
                    clients = clients.OrderBy(s => s.Status);
                    break;
                case "status_desc":
                    clients = clients.OrderByDescending(s => s.Status);
                    break;
                default:
                    clients = clients.OrderBy(s => s.CreatedDate);
                    break;
            }
            int pageSize = 10;
            var paginated = await TestingDemo.Models.PaginatedList<TestingDemo.Models.ClientModel>.CreateAsync(clients.AsNoTracking(), pageNumber ?? 1, pageSize);
            return Json(paginated);
        }
    }
}