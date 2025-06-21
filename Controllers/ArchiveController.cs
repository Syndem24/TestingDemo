using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;

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
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

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
    }
}