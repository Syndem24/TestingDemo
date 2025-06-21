using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ===== Auto Migrate and Seed Roles & Admin User =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    context.Database.EnsureCreated(); // Ensure database is created

    await CreateRoles(services);
    await CreateAdminUserAsync(services);
}

// ===== Middleware Pipeline =====
app.UseStaticFiles();
app.UseRouting();

// ✅ Cache Prevention Middleware — must be before authentication
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            return Task.CompletedTask;
        });
    }

    await next();
});


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();

// ===== Helper Methods =====
async Task CreateRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Admin", "Accountant", "User" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

async Task CreateAdminUserAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var scopedServices = scope.ServiceProvider;

    try
    {
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

        string adminRole = "Admin";
        string adminEmail = "admin@cpcpa.com";
        string adminPassword = "Admin@123";

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                // Add values for all required fields
                FullName = "Admin User",
                Age = 30,
                BirthDate = DateTime.Now,
                Address = "Admin Address",
                City = "Admin City",
                State = "Admin State",
                ZipCode = "12345",
                Country = "Admin Country"
            };

            var result = await userManager.CreateAsync(newAdmin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, adminRole);
                Console.WriteLine("✅ Admin user created successfully!");
            }
            else
            {
                Console.WriteLine("❌ Failed to create admin user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Error seeding admin user: " + ex.Message);
    }
}
