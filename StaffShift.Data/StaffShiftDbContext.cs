using StaffShift.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StaffShift.Data;

/// <summary>
/// Database context for the StaffShift application
/// </summary>
public class StaffShiftDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public StaffShiftDbContext(DbContextOptions<StaffShiftDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public override DbSet<User> Users { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<TimeOffRequest> TimeOffRequests { get; set; }
    public DbSet<ForumPost> ForumPosts { get; set; }
    public DbSet<ForumComment> ForumComments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<RegistrationRecord> RegistrationRecords { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserName).HasMaxLength(50);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            
            // Self-referencing relationship for Manager-Worker
            entity.HasOne(e => e.Manager)
                  .WithMany(m => m.Subordinates)
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Shift entity
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue("Scheduled");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Shifts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.ShiftDate });
        });

        // Configure TimeOffRequest entity
        modelBuilder.Entity<TimeOffRequest>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.RequestType).HasDefaultValue("Vacation");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.TimeOffRequests)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReviewedByUser)
                  .WithMany(u => u.ApprovedRequests)
                  .HasForeignKey(e => e.ReviewedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        // Configure ForumPost entity
        modelBuilder.Entity<ForumPost>(entity =>
        {
            entity.Property(e => e.Category).HasDefaultValue("Discussion");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ForumPosts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LastEditedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.LastEditedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure ForumComment entity
        modelBuilder.Entity<ForumComment>(entity =>
        {
            entity.HasOne(e => e.ForumPost)
                  .WithMany(p => p.Comments)
                  .HasForeignKey(e => e.ForumPostId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ForumComments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Type).HasDefaultValue("Info");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });

        // Configure LoginAttempt entity
        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => new { e.IpAddress, e.AttemptedAt });
        });
    }
}