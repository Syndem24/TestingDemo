using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TaskController : BaseController
{
    private readonly ApplicationDbContext _context;

    public TaskController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string sort)
    {
        // Fetch tasks from the database
        var tasks = _context.Tasks.AsQueryable();

        // Apply sorting based on the 'sort' parameter
        switch (sort)
        {
            case "client":
                tasks = tasks.OrderBy(t => t.ClientName);
                break;
            case "permit":
                tasks = tasks.OrderBy(t => t.Permit);
                break;
            case "requirements":
                tasks = tasks.OrderBy(t => t.Requirements);
                break;
            case "progress":
                tasks = tasks.OrderBy(t => t.Progress);
                break;
            case "priority":
                tasks = tasks.OrderBy(t => t.Priority);
                break;
            case "issued":
                tasks = tasks.OrderBy(t => t.DateIssued);
                break;
            default:
                tasks = tasks.OrderByDescending(t => t.DateIssued); // Default sorting
                break;
        }

        ViewBag.Tasks = tasks.ToList();
        return View();
    }

    public async Task<IActionResult> TaskFlow()
    {
        var tasks = await _context.Tasks.Where(t => !t.IsArchived).ToListAsync();
        ViewBag.Tasks = tasks;
        return View();
    }

    public IActionResult AddTask()
    {
        return View(new AddTaskViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> AddTask(AddTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newTask = new TaskFlowModel
        {
            ClientName = model.ClientName,
            Permit = model.Permit,
            Requirements = string.Join(",", model.Requirements ?? new List<string>()),
            Progress = 0,
            Priority = model.Priority,
            DateIssued = DateTime.Now,
            IsDone = false
        };

        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();

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
    // ...existing code...

    public async Task<IActionResult> DeleteConfirmation(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return View(task);
    }

    [HttpPost, ActionName("DeleteConfirmed")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Task deleted successfully.";
        }
        return RedirectToAction(nameof(TaskFlow));
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

    [HttpPost]
    public async Task<IActionResult> EditTask(TaskFlowModel model)
    {
        Console.WriteLine($"Progress received: {model.Progress}");
        Console.WriteLine($"Requirements: {model.Requirements}");
        Console.WriteLine($"DoneRequirements: {model.DoneRequirements}");
        Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");
        ModelState.Clear();

        // Only one validation check
        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState errors:");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }
            return View(model);
        }

        var task = await _context.Tasks.FindAsync(model.Id);
        if (task == null)
        {
            return NotFound();
        }

        // Ensure defaults for null values
        task.ClientName = model.ClientName ?? "";
        task.Permit = model.Permit ?? "";
        task.Requirements = model.Requirements ?? "";
        task.DoneRequirements = model.DoneRequirements ?? "";
        task.Priority = model.Priority ?? "Normal";

        // Always calculate progress based on requirements
        if (string.IsNullOrEmpty(task.Requirements))
        {
            task.Progress = 0;
        }
        else
        {
            var completed = !string.IsNullOrEmpty(task.DoneRequirements)
                ? task.DoneRequirements.Split(',', StringSplitOptions.RemoveEmptyEntries).Length
                : 0;

            var total = task.Requirements.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            task.Progress = (total > 0) ? (int)Math.Round((double)completed / total * 100) : 0;
        }

        Console.WriteLine($"Calculated progress: {task.Progress}");

        try
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            Console.WriteLine("Task saved successfully");

            TempData["SuccessMessage"] = "Task updated successfully!";
            return RedirectToAction("TaskFlow");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving task: {ex.Message}");
            ModelState.AddModelError("", "An error occurred while saving the task.");
            return View(model);
        }
    }

    // Calculate Progress - Added more detailed logging
    private int CalculateProgress(string doneRequirements, string allRequirements)
    {
        Console.WriteLine($"Calculating progress. Done requirements: '{doneRequirements}', All requirements: '{allRequirements}'");

        if (string.IsNullOrEmpty(allRequirements))
        {
            Console.WriteLine("All requirements is null or empty, returning 0");
            return 0;
        }

        var completedArray = doneRequirements?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var totalArray = allRequirements.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var completed = completedArray.Length;
        var total = totalArray.Length;

        Console.WriteLine($"Completed items: {completed}, Total items: {total}");

        int result = (total > 0) ? (int)Math.Round((double)completed / total * 100) : 0;
        Console.WriteLine($"Calculated progress: {result}%");
        return result;
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
