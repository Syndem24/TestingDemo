using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TaskController : Controller
{
    private readonly ApplicationDbContext _context;

    public TaskController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> TaskFlow()
    {
        var tasks = await _context.Tasks.Where(t => !t.IsArchived).ToListAsync();
        ViewBag.Tasks = tasks;
        return View();
    }

    public IActionResult AddTask()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(string clientName, string permit, string requirements, int progress, string priority)
    {
        if (!string.IsNullOrWhiteSpace(clientName) && !string.IsNullOrWhiteSpace(permit))
        {
            var newTask = new TaskFlowModel
            {
                ClientName = clientName,
                Permit = permit,
                Requirements = requirements,
                Progress = progress,
                Priority = priority,
                DateIssued = DateTime.Now,
                IsDone = false
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("TaskFlow");
    }

    public async Task<IActionResult> RestoreTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            task.IsDone = false;
            task.IsArchived = false;
            task.Progress = 0;
            task.DateCompleted = null;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("TaskFlow");
    }

    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("TaskFlow");
    }

    public async Task<IActionResult> MarkAsDone(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            task.Progress = 100;
            task.IsDone = true;
            task.DateCompleted = DateTime.Now;
            task.IsArchived = true;

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("TaskFlow");
        }
        return NotFound();
    }

    // GET: Edit Task
    public async Task<IActionResult> EditTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        var model = new TaskFlowModel
        {
            Id = task.Id,
            ClientName = task.ClientName,
            Permit = task.Permit,
            Requirements = task.Requirements, // Store as string
            DoneRequirements = task.DoneRequirements, // Store as string
            Progress = task.Progress,
            Priority = task.Priority
        };

        return View(model);
    }

    // POST: Save Edited Task
    [HttpPost]
    public async Task<IActionResult> EditTask(TaskFlowModel model)
    {
        Console.WriteLine($"Progress received: {model.Progress}"); // Add this
        Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}"); // Add this

        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState errors:"); // Add this
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }
            return View(model);
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var task = await _context.Tasks.FindAsync(model.Id);
        if (task == null)
        {
            return NotFound();
        }

        // Update fields
        task.ClientName = model.ClientName;
        task.Permit = model.Permit;
        task.Requirements = model.Requirements; // Keep as CSV string
        task.DoneRequirements = model.DoneRequirements; // Keep as CSV string
        task.Progress = CalculateProgress(model.DoneRequirements, model.Requirements);
        task.Priority = model.Priority;

        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Task updated successfully!";
        return RedirectToAction("TaskFlow");
    }

    // Calculate Progress
    private int CalculateProgress(string doneRequirements, string allRequirements)
    {
        if (string.IsNullOrEmpty(allRequirements)) return 0;

        var completed = doneRequirements?.Split(',', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
        var total = allRequirements.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
        return (total > 0) ? (int)Math.Round((double)completed / total * 100) : 0;
    }

    // Retrieve Requirement List
    private List<string> GetRequirementList()
    {
        return new List<string>
        {
            "ID Proof",
            "Application Form",
            "Tax Documents",
            "Business Registration",
            "Payment Receipt"
        };
    }

    public async Task<IActionResult> Archive()
    {
        var archivedTasks = await _context.Tasks.Where(t => t.IsArchived).ToListAsync();
        ViewBag.ArchivedTasks = archivedTasks;
        return View();
    }

    public async Task<IActionResult> Analytics()
    {
        try
        {
            var allTasks = await _context.Tasks.ToListAsync();
            ViewBag.AllTasks = allTasks;
            ViewBag.TotalPermits = allTasks.Count;
            ViewBag.CompletedPermits = allTasks.Count(t => t.IsDone);
            ViewBag.InProgressPermits = allTasks.Count(t => !t.IsDone && !t.IsArchived);
            ViewBag.ArchivedPermits = allTasks.Count(t => t.IsArchived);

            var completedTasks = allTasks.Where(t => t.DateCompleted.HasValue).ToList();
            ViewBag.AvgCompletionDays = completedTasks.Any()
                ? Math.Round(completedTasks.Average(t => (t.DateCompleted.Value - t.DateIssued).TotalDays), 1)
                : 0;

            ViewBag.PriorityData = allTasks
                .GroupBy(t => string.IsNullOrEmpty(t.Priority) ? "Not Set" : t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .OrderBy(x => x.Priority)
                .ToList();

            ViewBag.MonthlyData = allTasks
                .GroupBy(t => new { t.DateIssued.Month, t.DateIssued.Year })
                .Select(g => new { MonthYear = $"{g.Key.Month}/{g.Key.Year}", Count = g.Count() })
                .OrderBy(x => x.MonthYear)
                .ToList();

            return View();
        }
        catch (Exception)
        {
            ViewBag.ErrorMessage = "An error occurred while generating analytics. Please try again later.";
            return View("Error");
        }
    }
}
