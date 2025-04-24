using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationUser : IdentityUser
{
    // Add custom user properties if needed
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Ensure ApplicationUser is registered
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<TaskFlowModel> Tasks { get; set; }

    public DbSet<AcquiringRequestModel> AcquiringRequests { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Additional model configurations if needed
    }
}
