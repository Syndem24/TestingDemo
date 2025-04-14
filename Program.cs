using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Apply migrations automatically and seed admin user
using (var scope = app.Services.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var context = scopedServices.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    // Ensure admin user is created
    await CreateAdminUserAsync(scopedServices);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
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

using (var scope = app.Services.CreateScope())
{
    await CreateRoles(scope.ServiceProvider);
}
// Function to create admin user
async Task CreateAdminUserAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var scopedServices = scope.ServiceProvider;

    try
    {
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

        string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        string adminEmail = "admin@cpcpa.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            string adminPassword = "Admin@123";
            var result = await userManager.CreateAsync(newAdmin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, adminRole);
                Console.WriteLine("✅ Admin user created successfully!");
            }
            else
            {
                Console.WriteLine("❌ Failed to create admin user: " + string.Join(", ", result.Errors));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Error seeding admin user: " + ex.Message);
    }
}
