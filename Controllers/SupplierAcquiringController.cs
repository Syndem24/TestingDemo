using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class SupplierAcquiringController : Controller
{
    private readonly ApplicationDbContext _context;

    public SupplierAcquiringController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(new AcquiringRequestModel { ContactPerson = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(AcquiringRequestModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        _context.AcquiringRequests.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your request has been submitted successfully. Our team will review and contact you soon.";
        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return View();
    }
}