using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;

namespace TestingDemo.Controllers
{
    [Authorize] // Requires authentication
    public class PlanningOfficerController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PlanningOfficerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlanningOfficer/Index
        public async Task<IActionResult> Index(int? pageNumber)
        {
            ViewData["Title"] = "Planning Clients";
            ViewData["ListTitle"] = "Planning Clients";
            ViewData["CurrentAction"] = "Index";

            int pageSize = 10;
            var clientsQuery = _context.Clients
                .Where(c => c.Status == "Planning")
                .AsNoTracking()
                .OrderBy(c => c.CreatedDate);

            var paginatedClients = await PaginatedList<ClientModel>.CreateAsync(clientsQuery, pageNumber ?? 1, pageSize);

            var clientIds = paginatedClients.Select(c => c.Id).ToList();
            var requirements = await _context.PermitRequirements
                .Where(r => clientIds.Contains(r.ClientId))
                .ToListAsync();

            ViewBag.Requirements = requirements
                .GroupBy(r => r.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View("PlanningClients", paginatedClients);
        }

        // GET: PlanningOfficer/PlanningClients
        public async Task<IActionResult> PlanningClients(int? pageNumber)
        {
            ViewData["Title"] = "Pending Clients";
            ViewData["ListTitle"] = "Pending Clients";
            ViewData["CurrentAction"] = "PlanningClients";

            int pageSize = 10;
            var clientsQuery = _context.Clients
                .Where(c => c.Status == "Pending" || c.Status == "Finance")
                .AsNoTracking()
                .OrderBy(c => c.CreatedDate);

            var paginatedClients = await PaginatedList<ClientModel>.CreateAsync(clientsQuery, pageNumber ?? 1, pageSize);

            var clientIds = paginatedClients.Select(c => c.Id).ToList();
            var requirements = await _context.PermitRequirements
                .Where(r => clientIds.Contains(r.ClientId))
                .ToListAsync();

            ViewBag.Requirements = requirements
                .GroupBy(r => r.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(paginatedClients);
        }

        // GET: PlanningOfficer/CompletedClients
        public async Task<IActionResult> CompletedClients(int? pageNumber)
        {
            ViewData["Title"] = "Completed Clients";
            ViewData["ListTitle"] = "Completed by Planning";
            ViewData["CurrentAction"] = "CompletedClients"; // For navigation active state

            int pageSize = 10;
            var clientsQuery = _context.Clients
                .Where(c => c.Status == "Completed")
                .OrderBy(c => c.CreatedDate)
                .AsNoTracking();

            var paginatedClients = await PaginatedList<ClientModel>.CreateAsync(clientsQuery, pageNumber ?? 1, pageSize);

            return View(paginatedClients);
        }

        // GET: PlanningOfficer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.RetainershipBIR)
                .Include(c => c.RetainershipSPP)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (client == null)
            {
                return NotFound();
            }

            // Get requirements for this client
            ViewBag.Requirements = await _context.PermitRequirements
                .Where(r => r.ClientId == id)
                .ToListAsync();

            return View(client);
        }

        // GET: PlanningOfficer/PlanRequirements/5
        public async Task<IActionResult> PlanRequirements(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.RetainershipBIR)
                .Include(c => c.RetainershipSPP)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            // Check if we need to import requirements from Finance
            var existingRequirements = await _context.PermitRequirements
                .Where(r => r.ClientId == id)
                .ToListAsync();

            // Update client status
            client.Status = "Planning";
            await _context.SaveChangesAsync();

            // Send requirements to view
            ViewBag.Requirements = existingRequirements;

            return View(client);
        }

        // Simple action for direct adding (no validation)
        // GET: PlanningOfficer/QuickAddRequirement/5?name=Test&description=Description
        public async Task<IActionResult> QuickAddRequirement(int id, string name, string description)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
            {
                return BadRequest("Name and description are required");
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound("Client not found");
            }

            var requirement = new PermitRequirementModel
            {
                ClientId = id,
                RequirementName = name,
                Description = description,
                IsRequired = true,
                CreatedDate = DateTime.Now
            };

            _context.PermitRequirements.Add(requirement);
            await _context.SaveChangesAsync();

            return RedirectToAction("PlanRequirements", new { id });
        }

        // POST: PlanningOfficer/AddRequirement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRequirement([Bind("ClientId,RequirementName,Description,IsRequired")] PermitRequirementModel requirement)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Make sure the ClientId is valid
                    var client = await _context.Clients.FindAsync(requirement.ClientId);
                    if (client == null)
                    {
                        ModelState.AddModelError("ClientId", "Invalid client ID");
                        return View("PlanRequirements", client);
                    }

                    requirement.CreatedDate = DateTime.Now;
                    requirement.IsCompleted = false; // Always start as not completed

                    _context.PermitRequirements.Add(requirement);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Requirement added successfully!";
                    return RedirectToAction("PlanRequirements", new { id = requirement.ClientId });
                }
                else
                {
                    // Log validation errors for debugging
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            // You could log these errors
                            System.Diagnostics.Debug.WriteLine($"Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving the requirement.");
            }

            // If we get here, something went wrong
            // Get client for the view
            var clientModel = await _context.Clients.FindAsync(requirement.ClientId);

            // Get existing requirements for the view
            ViewBag.Requirements = await _context.PermitRequirements
                .Where(r => r.ClientId == requirement.ClientId)
                .ToListAsync();

            // Store the invalid model in TempData
            TempData["RequirementData"] = new
            {
                Name = requirement.RequirementName,
                Description = requirement.Description,
                IsRequired = requirement.IsRequired
            };

            TempData["ErrorMessage"] = "Failed to add requirement. Please check your input.";

            return View("PlanRequirements", clientModel);
        }

        // GET: PlanningOfficer/EditRequirement/5
        public async Task<IActionResult> EditRequirement(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requirement = await _context.PermitRequirements.FindAsync(id);
            if (requirement == null)
            {
                return NotFound();
            }

            // Get client information
            ViewBag.Client = await _context.Clients.FindAsync(requirement.ClientId);

            return View(requirement);
        }

        // POST: PlanningOfficer/EditRequirement/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequirement(int id, PermitRequirementModel requirement)
        {
            if (id != requirement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(requirement);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Requirement updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequirementExists(requirement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("PlanRequirements", new { id = requirement.ClientId });
            }

            // Get client information
            ViewBag.Client = await _context.Clients.FindAsync(requirement.ClientId);

            return View(requirement);
        }

        // POST: PlanningOfficer/DeleteRequirement/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequirement(int id)
        {
            var requirement = await _context.PermitRequirements.FindAsync(id);

            if (requirement != null)
            {
                int clientId = requirement.ClientId;
                _context.PermitRequirements.Remove(requirement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Requirement deleted successfully!";
                return RedirectToAction("PlanRequirements", new { id = clientId });
            }

            return RedirectToAction("Index");
        }

        // POST: PlanningOfficer/CompleteRequirements/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequirements(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client != null)
            {
                client.Status = "Completed";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Requirements planning completed successfully!";
            }

            return RedirectToAction("Index");
        }

        // POST: PlanningOfficer/ToggleRequirementStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleRequirementStatus(int id, bool isCompleted)
        {
            try
            {
                var requirement = await _context.PermitRequirements.FindAsync(id);
                if (requirement == null)
                {
                    return Json(new { success = false, message = "Requirement not found" });
                }

                // Update completion status
                requirement.IsCompleted = isCompleted;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: PlanningOfficer/ProceedToLiaison/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToLiaison(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            client.Status = "Liaison"; // Status updated to Liaison
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Client {client.ClientName} has been proceeded to Liaison.";
            return RedirectToAction("Index");
        }

        // POST: PlanningOfficer/BackToFinance/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackToFinance(int id, string note)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                client.Status = "Pending"; // Changed from "Finance" to "Pending"
                client.PlanningReturnNote = note; // Save the note for Finance to see
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Client returned to Finance's pending list. Note: {note}";
            }
            return RedirectToAction("Index");
        }

        private bool RequirementExists(int id)
        {
            return _context.PermitRequirements.Any(e => e.Id == id);
        }
    }
}