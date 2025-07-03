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
    public string? ContactPersonNumber { get; set; } // Employee contact number
    public bool IsApproved { get; set; } = true;
}


public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Ensure ApplicationUser is registered
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }

    public DbSet<AcquiringRequestModel> AcquiringRequests { get; set; }

    public DbSet<ClientModel> Clients { get; set; }

    public DbSet<PermitRequirementModel> PermitRequirements { get; set; }

    public DbSet<RetainershipBIRModel> RetainershipBIRs { get; set; }
    public DbSet<RetainershipSPPModel> RetainershipSPPs { get; set; }
    public DbSet<OneTimeTransactionModel> OneTimeTransactions { get; set; }
    public DbSet<ExternalAuditModel> ExternalAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure the relationship between Client and PermitRequirement
        builder.Entity<PermitRequirementModel>()
            .HasOne(pr => pr.Client)
            .WithMany()
            .HasForeignKey(pr => pr.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
