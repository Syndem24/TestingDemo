using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestingDemo.Models;
using Microsoft.AspNetCore.SignalR;
using TestingDemo.Data;

namespace TestingDemo.Controllers
{
    [Authorize(Roles = "PlanningOfficer,Admin")]
    public class PlanningOfficerController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public PlanningOfficerController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                .Include(c => c.RetainershipBIR)
                .Include(c => c.RetainershipSPP)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .OrderBy(c => c.CreatedDate)
                .AsNoTracking();

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

        // GET: PlanningOfficer/PlanningClients (deprecated) -> Redirect to Index (Planning)
        public IActionResult PlanningClients(int? pageNumber)
        {
            return RedirectToAction(nameof(Index), new { pageNumber });
        }

        // GET: PlanningOfficer/CompletedClients (deprecated) -> Redirect to Index (Planning)
        public IActionResult CompletedClients(int? pageNumber)
        {
            return RedirectToAction(nameof(Index), new { pageNumber });
        }

        // Details action removed - details now handled via modal in the Planning Officer views

        // GET: PlanningOfficer/PlanRequirements/5
        public async Task<IActionResult> PlanRequirements(int? id)
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

            // Ensure status is Planning
            if (client.Status != "Planning")
            {
                client.Status = "Planning";
                await _context.SaveChangesAsync();
            }

            // Redirect to PlanningClients modal flow
            TempData["OpenClientId"] = id.ToString();
            return RedirectToAction("Index");
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

            TempData["SuccessMessage"] = "Requirement added successfully!";
            TempData["OpenClientId"] = id.ToString();
            return RedirectToAction("Index");
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
                        // Fall through to error handling below so we redirect back with TempData
                    }

                    if (ModelState.IsValid)
                    {
                        requirement.CreatedDate = DateTime.Now;
                        requirement.IsCompleted = false; // Always start as not completed

                        _context.PermitRequirements.Add(requirement);
                        await _context.SaveChangesAsync();
                        await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

                        TempData["SuccessMessage"] = "Requirement added successfully!";
                        TempData["OpenClientId"] = requirement.ClientId.ToString();
                        var returnUrl = Request?.Form["returnUrl"].ToString();
                        if (!string.IsNullOrWhiteSpace(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // Log validation errors for debugging
                    foreach (var kvp in ModelState)
                    {
                        foreach (var error in kvp.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"ModelState Error on {kvp.Key}: {error.ErrorMessage}");
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
            // Store simple values in TempData (no anonymous objects)
            TempData["RequirementName"] = requirement.RequirementName ?? string.Empty;
            TempData["RequirementDescription"] = requirement.Description ?? string.Empty;
            TempData["RequirementIsRequired"] = requirement.IsRequired ? "true" : "false";
            TempData["ErrorMessage"] = "Failed to add requirement. Please check your input.";
            TempData["OpenClientId"] = requirement.ClientId.ToString();
            // Collect validation errors for display
            var errors = ModelState
                .Where(kvp => kvp.Value.Errors.Any())
                .Select(kvp => $"{kvp.Key}: {string.Join("; ", kvp.Value.Errors.Select(e => e.ErrorMessage))}")
                .ToList();
            if (errors.Any())
            {
                TempData["ValidationErrors"] = string.Join(" | ", errors);
            }

            var backUrl = Request?.Form["returnUrl"].ToString();
            if (!string.IsNullOrWhiteSpace(backUrl))
            {
                return Redirect(backUrl);
            }
            return RedirectToAction("Index");
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
                    var existing = await _context.PermitRequirements.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }
                    existing.RequirementName = requirement.RequirementName;
                    existing.Description = requirement.Description;
                    existing.IsRequired = requirement.IsRequired;
                    existing.IsCompleted = requirement.IsCompleted;

                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

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
                TempData["OpenClientId"] = requirement.ClientId.ToString();
                return RedirectToAction("Index");
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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

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
            await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");

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
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", "PlanningOfficer data changed");
                TempData["SuccessMessage"] = $"Client returned to Finance's pending list. Note: {note}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestData(int? pageNumber)
        {
            int pageSize = 10;
            var clientsQuery = _context.Clients
                .Where(c => c.Status == "Planning")
                .Include(c => c.RetainershipBIR)
                .Include(c => c.RetainershipSPP)
                .Include(c => c.OneTimeTransaction)
                .Include(c => c.ExternalAudit)
                .OrderBy(c => c.CreatedDate)
                .AsNoTracking();
            var paginatedClients = await PaginatedList<ClientModel>.CreateAsync(clientsQuery, pageNumber ?? 1, pageSize);
            var clientIds = paginatedClients.Select(c => c.Id).ToList();
            var requirements = await _context.PermitRequirements
                .Where(r => clientIds.Contains(r.ClientId))
                .ToListAsync();
            var requirementsByClient = requirements
                .GroupBy(r => r.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());
            return Json(new { Clients = paginatedClients, RequirementsByClient = requirementsByClient });
        }

        // GET: PlanningOfficer/Requirements/{id}
        [HttpGet]
        public async Task<IActionResult> Requirements(int id)
        {
            var requirements = await _context.PermitRequirements
                .Where(r => r.ClientId == id)
                .OrderBy(r => r.Id)
                .AsNoTracking()
                .ToListAsync();

            return PartialView("_RequirementsList", requirements);
        }

        private bool RequirementExists(int id)
        {
            return _context.PermitRequirements.Any(e => e.Id == id);
        }
    }
}