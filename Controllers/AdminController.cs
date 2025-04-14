using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> AddUser()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(string email, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
        {
            ViewBag.Error = "All fields are required.";
            return View();
        }

        var userExists = await _userManager.FindByEmailAsync(email);
        if (userExists != null)
        {
            ViewBag.Error = "User already exists.";
            return View();
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
            await _userManager.AddToRoleAsync(user, role);
            ViewBag.Success = "User added successfully!";
        }
        else
        {
            ViewBag.Error = "Failed to add user: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return View();
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var userList = users.Select(user => new
        {
            user.Email,
            Roles = _userManager.GetRolesAsync(user).Result
        }).ToList();

        ViewBag.Users = userList;
        return View();
    }
    // GET: Edit User
    public async Task<IActionResult> EditUser(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction("Users");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return RedirectToAction("Users");
        }

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", roles.FirstOrDefault());

        return View(user);
    }
    // POST: Edit User
    [HttpPost]
    public async Task<IActionResult> EditUser(string email, string newEmail, string role, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return RedirectToAction("Users");
        }

        // Update email if changed
        if (!string.IsNullOrWhiteSpace(newEmail) && email != newEmail)
        {
            user.Email = newEmail;
            user.UserName = newEmail;
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, resetToken, password);
        }

        // Update role
        var existingRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, existingRoles);
        await _userManager.AddToRoleAsync(user, role);

        await _userManager.UpdateAsync(user);
        return RedirectToAction("Users");
    }


    // DELETE: Delete User
    public async Task<IActionResult> DeleteUser(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }

        return RedirectToAction("Users");
    }

}
