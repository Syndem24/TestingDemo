using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using TestingDemo.Models;

public class ApplicationUser : IdentityUser
{
    // Add custom user properties
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
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
