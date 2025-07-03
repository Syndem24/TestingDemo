using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // ✅ Add this line
using Microsoft.EntityFrameworkCore;
using TestingDemo.ViewModels;
using TestingDemo.Models; // Add this for ApplicationUser

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult AddUser()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(
        string email,
        string password,
        string fullName,
        int age,
        DateTime birthDate,
        string address,
        string city,
        string state,
        string zipCode,
        string country,
        string contactNumber,
        List<string> roles)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ViewBag.Error = "User with this email already exists.";
                return View();
            }

            // Create new user with all required fields
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Age = age,
                BirthDate = birthDate,
                Address = address,
                City = city,
                State = state,
                ZipCode = zipCode,
                Country = country,
                ContactPersonNumber = contactNumber
            };

            // Create the user
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assign all selected roles
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    await _userManager.AddToRoleAsync(user, role);
                }
                ViewBag.Success = $"User {email} created successfully with roles: {string.Join(", ", roles)}.";
            }
            else
            {
                ViewBag.Error = "Error creating user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "An error occurred: " + ex.Message;
        }

        return View();
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "No Role"
            });
        }

        return View(userViewModels); // ✅ Strongly typed view
    }
    public async Task<IActionResult> UserDetails(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            Age = user.Age,
            BirthDate = user.BirthDate,
            Address = user.Address ?? string.Empty,
            City = user.City ?? string.Empty,
            State = user.State ?? string.Empty,
            ZipCode = user.ZipCode ?? string.Empty,
            Country = user.Country ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "No Role"
        };

        return View(model);
    }
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            Age = user.Age,
            BirthDate = user.BirthDate,
            Address = user.Address ?? string.Empty,
            City = user.City ?? string.Empty,
            State = user.State ?? string.Empty,
            ZipCode = user.ZipCode ?? string.Empty,
            Country = user.Country ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "No Role"
        };

        ViewBag.Roles = new SelectList((await _roleManager.Roles.Select(r => r.Name).ToListAsync()).Where(r => r != "Accountant"));
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(UserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Age = model.Age;
        user.BirthDate = model.BirthDate;
        user.Address = model.Address;
        user.City = model.City;
        user.State = model.State;
        user.ZipCode = model.ZipCode;
        user.Country = model.Country;

        var existingRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!string.IsNullOrEmpty(model.Role))
            await _userManager.AddToRoleAsync(user, model.Role);

        await _userManager.UpdateAsync(user);
        return RedirectToAction("Users");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Users");
            }
        }
        return RedirectToAction("Users");
    }
    // GET: Confirm Delete Page
    public async Task<IActionResult> ConfirmDelete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["ErrorMessage"] = "Invalid user ID.";
            return RedirectToAction("Users");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Users");
        }

        // Pass the user details to the view
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "No Role"
        };

        return View(viewModel);
    }

    // POST: Handle Deletion
    [HttpPost]
    public async Task<IActionResult> ConfirmDelete(UserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Users");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "User deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete user.";
        }

        return RedirectToAction("Users");
    }

    public async Task<IActionResult> PendingApprovals()
    {
        var users = await _userManager.Users.Where(u => !u.IsApproved).ToListAsync();
        var userViewModels = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "No Role"
            });
        }
        return View(userViewModels);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            user.IsApproved = true;
            await _userManager.UpdateAsync(user);
        }
        return RedirectToAction("PendingApprovals");
    }
}

// View model for displaying users
