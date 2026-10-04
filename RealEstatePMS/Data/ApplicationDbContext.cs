using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Rental> Rentals => Set<Rental>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ProjectStatusHistory> ProjectStatusHistories => Set<ProjectStatusHistory>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.HasOne(p => p.ProjectManager)
                  .WithMany(u => u.ManagedProjects)
                  .HasForeignKey(p => p.ProjectManagerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Property>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.HasOne(p => p.Project)
                  .WithMany(pr => pr.Properties)
                  .HasForeignKey(p => p.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PropertyImage>(entity =>
        {
            entity.HasOne(pi => pi.Property)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Phone);
            entity.HasOne(c => c.ApplicationUser)
                  .WithMany()
                  .HasForeignKey(c => c.ApplicationUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Sale>(entity =>
        {
            entity.HasOne(s => s.Customer)
                  .WithMany(c => c.Sales)
                  .HasForeignKey(s => s.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Property)
                  .WithOne(p => p.Sale)
                  .HasForeignKey<Sale>(s => s.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.SalesAgent)
                  .WithMany(u => u.Sales)
                  .HasForeignKey(s => s.SalesAgentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Rental>(entity =>
        {
            entity.HasOne(r => r.Customer)
                  .WithMany(c => c.Rentals)
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Property)
                  .WithOne(p => p.Rental)
                  .HasForeignKey<Rental>(r => r.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Customer)
                  .WithMany(c => c.Payments)
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Sale)
                  .WithMany(s => s.Payments)
                  .HasForeignKey(p => p.SaleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Rental)
                  .WithMany(r => r.Payments)
                  .HasForeignKey(p => p.RentalId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectTask>(entity =>
        {
            entity.HasOne(t => t.Project)
                  .WithMany(p => p.Tasks)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.AssignedEmployee)
                  .WithMany(u => u.AssignedTasks)
                  .HasForeignKey(t => t.AssignedEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Document>(entity =>
        {
            // Restrict (rather than cascade) on Project/Property/Sale/Rental to avoid SQL Server's
            // "multiple cascade paths" error, since Property already cascades from Project, etc.
            // Controllers remove a record's Documents explicitly before deleting the record itself.
            entity.HasOne(d => d.Project).WithMany(p => p.Documents).HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Property).WithMany(p => p.Documents).HasForeignKey(d => d.PropertyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Sale).WithMany(s => s.Documents).HasForeignKey(d => d.SaleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Rental).WithMany(r => r.Documents).HasForeignKey(d => d.RentalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.UploadedBy).WithMany().HasForeignKey(d => d.UploadedById).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BookingRequest>(entity =>
        {
            entity.HasOne(b => b.Customer)
                  .WithMany()
                  .HasForeignKey(b => b.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Property)
                  .WithMany()
                  .HasForeignKey(b => b.PropertyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.ReviewedBy)
                  .WithMany()
                  .HasForeignKey(b => b.ReviewedById)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.ResultingSale)
                  .WithMany()
                  .HasForeignKey(b => b.ResultingSaleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(b => b.ResultingRental)
                  .WithMany()
                  .HasForeignKey(b => b.ResultingRentalId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(p => new { p.Role, p.Module }).IsUnique();
        });

        builder.Entity<BookingRequest>(entity =>
        {
            entity.HasIndex(b => b.BookingNumber).IsUnique().HasFilter("[BookingNumber] <> ''");
        });

        builder.Entity<NotificationPreference>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Type }).IsUnique();
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Project)
                  .WithMany()
                  .HasForeignKey(h => h.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedBy)
                  .WithMany()
                  .HasForeignKey(h => h.ChangedById)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Decimal precision fallback for any unmapped decimal properties
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null)
                property.SetColumnType("decimal(18,2)");
        }
    }
}
