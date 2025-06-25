using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using TestingDemo.Models;

namespace TestingDemo.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // ✅ Add database context

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients.ToListAsync();

            var model = new TestingDemo.ViewModels.DashboardViewModel
            {
                LiaisonClients = clients.Where(c => c.Status == "Liaison").ToList(),
                FinanceClients = clients.Where(c => c.Status == "Pending" || c.Status == "Finance" || c.Status == "Clearance").ToList(),
                PlanningClients = clients.Where(c => c.Status == "Planning").ToList(),
                ReceivedClients = clients.Where(c => c.Status == "CustomerCareReceived").ToList(),
                DocumentationClients = clients.Where(c => c.Status == "DocumentOfficer").ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
