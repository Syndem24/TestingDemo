using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;

namespace TestingDemo.Controllers
{
    [Authorize(Roles = "DocumentOfficer,Admin")]
    public class DocumentOfficerController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public DocumentOfficerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DocumentOfficer/Index
        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients.Where(c => c.Status == "DocumentOfficer").ToListAsync();
            return View(clients);
        }

        // GET: DocumentOfficer/Details/5
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

            return View(client);
        }

        // POST: DocumentOfficer/ProceedToFinance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToFinance(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "Clearance";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client has been sent to Finance for clearance.";
            }
            return RedirectToAction("Index");
        }

        // POST: DocumentOfficer/ReturnToCustomerCare/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToCustomerCare(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "CustomerCareReceived";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client returned to Customer Care (Received).";
            }
            return RedirectToAction("Index");
        }
    }
}