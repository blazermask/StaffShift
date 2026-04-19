using StaffShift.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace StaffShift.Data;

/// <summary>
/// Initializes the database with default data
/// </summary>
public static class DataInitializer
{
    public static async Task SeedDataAsync(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager, StaffShiftDbContext context)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = { "CEO", "Manager", "Worker" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
            }
        }

        // Create default CEO account
        var ceoUser = await userManager.FindByNameAsync("ceo");
        if (ceoUser == null)
        {
            ceoUser = new User
            {
                UserName = "ceo",
                Email = "ceo@company.com",
                FirstName = "Chief",
                LastName = "Executive",
                EmployeeId = "CEO001",
                Department = "Executive",
                Position = "Chief Executive Officer",
                HireDate = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(ceoUser, "CEO123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(ceoUser, "CEO");
            }
        }

        // Create default Manager account
        var managerUser = await userManager.FindByNameAsync("manager");
        if (managerUser == null)
        {
            managerUser = new User
            {
                UserName = "manager",
                Email = "manager@company.com",
                FirstName = "John",
                LastName = "Manager",
                EmployeeId = "MGR001",
                Department = "Operations",
                Position = "Operations Manager",
                HireDate = DateTime.UtcNow.AddDays(-365),
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(managerUser, "Manager123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        // Create default Worker account
        var workerUser = await userManager.FindByNameAsync("worker");
        if (workerUser == null)
        {
            workerUser = new User
            {
                UserName = "worker",
                Email = "worker@company.com",
                FirstName = "Jane",
                LastName = "Worker",
                EmployeeId = "WRK001",
                Department = "Operations",
                Position = "Team Member",
                HireDate = DateTime.UtcNow.AddDays(-180),
                IsActive = true,
                EmailConfirmed = true,
                ManagerId = (await userManager.FindByNameAsync("manager"))?.Id
            };

            var result = await userManager.CreateAsync(workerUser, "Worker123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(workerUser, "Worker");
            }
        }

        await context.SaveChangesAsync();
    }
}