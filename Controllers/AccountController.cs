using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;
using System.Collections.Concurrent;
using TestingDemo.ViewModels;
using Microsoft.Extensions.Configuration;

public class AccountController : BaseController
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private static ConcurrentDictionary<string, (int FailCount, DateTime? BlockUntil)> _changePwAttempts = new();
    private readonly IConfiguration _config;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _config = config;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home"); // Redirect to dashboard
            }
        }
        ViewBag.Error = "Invalid login attempt.";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View(model);
        }
        // Rate limiting
        var key = user.Id;
        if (_changePwAttempts.TryGetValue(key, out var entry) && entry.BlockUntil.HasValue && entry.BlockUntil > DateTime.Now)
        {
            ModelState.AddModelError("", $"Too many failed attempts. Try again at {entry.BlockUntil.Value:HH:mm:ss}.");
            return View(model);
        }
        // Verify current password
        if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
        {
            _changePwAttempts.AddOrUpdate(key, (1, null), (k, v) => (v.FailCount + 1, v.FailCount + 1 >= 5 ? DateTime.Now.AddMinutes(15) : null));
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View(model);
        }
        // Password requirements
        if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 8 ||
            !model.NewPassword.Any(char.IsUpper) || !model.NewPassword.Any(char.IsLower) ||
            !model.NewPassword.Any(char.IsDigit) || !model.NewPassword.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            ModelState.AddModelError("NewPassword", "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
            return View(model);
        }
        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
            return View(model);
        }
        // Generate OTP
        var otp = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("ChangePwOtp", otp);
        HttpContext.Session.SetString("ChangePwNewPassword", model.NewPassword);
        HttpContext.Session.SetString("ChangePwCurrentPassword", model.CurrentPassword);
        HttpContext.Session.SetString("ChangePwOtpTime", DateTime.UtcNow.ToString("o"));
        // Email OTP
        try
        {
            var smtpSection = _config.GetSection("Smtp");
            var smtp = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
            {
                Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]),
                EnableSsl = true
            };
            var mail = new MailMessage(smtpSection["From"], user.Email)
            {
                Subject = "Password Change OTP",
                Body = $"Your password change code is: {otp}"
            };
            smtp.Send(mail);
        }
        catch (Exception ex) { System.IO.File.AppendAllText("email_error.log", ex.ToString() + "\n"); }
        TempData["OtpNotice"] = "A code has been sent to your email. Enter it to confirm your password change.";
        return RedirectToAction("ConfirmChangePassword");
    }

    [HttpGet]
    public IActionResult ConfirmChangePassword()
    {
        if (TempData["OtpNotice"] != null)
            ViewBag.OtpNotice = TempData["OtpNotice"];
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmChangePassword(string otp)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View();
        }
        var expectedOtp = HttpContext.Session.GetString("ChangePwOtp");
        var newPassword = HttpContext.Session.GetString("ChangePwNewPassword");
        var currentPassword = HttpContext.Session.GetString("ChangePwCurrentPassword");
        var otpTimeStr = HttpContext.Session.GetString("ChangePwOtpTime");
        if (expectedOtp == null || newPassword == null || currentPassword == null || otpTimeStr == null)
        {
            ModelState.AddModelError("", "OTP session expired. Please try again.");
            return View();
        }
        if ((DateTime.UtcNow - DateTime.Parse(otpTimeStr)).TotalMinutes > 2)
        {
            ModelState.AddModelError("", "OTP expired. Please try again.");
            return View();
        }
        if (otp != expectedOtp)
        {
            TempData["OtpError"] = "The OTP code you entered is incorrect. Please try again.";
            ModelState.AddModelError("", "Invalid code. Please check your email and try again.");
            return View();
        }
        // Change password
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", string.Join("; ", result.Errors.Select(e => e.Description)));
            return View();
        }
        // Reset rate limit
        _changePwAttempts.TryRemove(user.Id, out _);
        // Audit log
        System.IO.File.AppendAllText("password_change_audit.log", $"{DateTime.Now:u} | {user.Email} | {Request.HttpContext.Connection.RemoteIpAddress} | {Request.Headers["User-Agent"]} | OTP\n");
        // Email notification
        try
        {
            var smtpSection = _config.GetSection("Smtp");
            var smtp = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
            {
                Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]),
                EnableSsl = true
            };
            var mail = new MailMessage(smtpSection["From"], user.Email)
            {
                Subject = "Password Changed",
                Body = "Your password was changed. If this wasn't you, please contact support."
            };
            smtp.Send(mail);
        }
        catch (Exception ex) { System.IO.File.AppendAllText("email_error.log", ex.ToString() + "\n"); }
        // Sign out all sessions except current
        await _signInManager.RefreshSignInAsync(user);
        // Clear session
        HttpContext.Session.Remove("ChangePwOtp");
        HttpContext.Session.Remove("ChangePwNewPassword");
        HttpContext.Session.Remove("ChangePwCurrentPassword");
        HttpContext.Session.Remove("ChangePwOtpTime");
        ViewBag.Success = "Password changed successfully.";
        TempData["PasswordChanged"] = "Your password has been changed successfully!";
        return RedirectToAction("Settings");
    }

    [HttpPost]
    public IActionResult ResendChangePasswordOtp()
    {
        var user = _userManager.GetUserAsync(User).Result;
        if (user == null)
        {
            TempData["OtpNotice"] = "User not found.";
            return RedirectToAction("ConfirmChangePassword");
        }
        // Rate limit: allow max 3 resends per 10 minutes
        var resendKey = $"ResendOtp_{user.Id}";
        var resendCount = HttpContext.Session.GetInt32(resendKey) ?? 0;
        var resendTimeKey = $"ResendOtpTime_{user.Id}";
        var lastResendStr = HttpContext.Session.GetString(resendTimeKey);
        if (lastResendStr != null && DateTime.TryParse(lastResendStr, out var lastResend))
        {
            if ((DateTime.UtcNow - lastResend).TotalMinutes < 10 && resendCount >= 3)
            {
                TempData["OtpNotice"] = "You have reached the maximum number of resends. Please try again later.";
                return RedirectToAction("ConfirmChangePassword");
            }
            if ((DateTime.UtcNow - lastResend).TotalMinutes >= 10)
            {
                resendCount = 0; // Reset after 10 minutes
            }
        }
        // Generate new OTP
        var otp = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("ChangePwOtp", otp);
        HttpContext.Session.SetString("ChangePwOtpTime", DateTime.UtcNow.ToString("o"));
        // Email OTP
        try
        {
            var smtpSection = _config.GetSection("Smtp");
            var smtp = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
            {
                Credentials = new NetworkCredential(smtpSection["Username"], smtpSection["Password"]),
                EnableSsl = true
            };
            var mail = new MailMessage(smtpSection["From"], user.Email)
            {
                Subject = "Password Change OTP",
                Body = $"Your new password change code is: {otp}"
            };
            smtp.Send(mail);
        }
        catch (Exception ex) { System.IO.File.AppendAllText("email_error.log", ex.ToString() + "\n"); }
        HttpContext.Session.SetInt32(resendKey, resendCount + 1);
        HttpContext.Session.SetString(resendTimeKey, DateTime.UtcNow.ToString("o"));
        TempData["OtpNotice"] = "A new code has been sent to your email.";
        return RedirectToAction("ConfirmChangePassword");
    }
}
