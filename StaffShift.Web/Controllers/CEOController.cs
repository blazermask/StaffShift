using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Controller for CEO company-wide management operations
/// </summary>
[Authorize(Roles = "CEO")]
public class CEOController : Controller
{
    private readonly IUserService _userService;
    private readonly IShiftService _shiftService;
    private readonly ITimeOffService _timeOffService;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public CEOController(
        IUserService userService, 
        IShiftService shiftService, 
        ITimeOffService timeOffService, 
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _userService = userService;
        _shiftService = shiftService;
        _timeOffService = timeOffService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: CEO/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var users = await _userService.GetAllUsersAsync();
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // Calculate statistics
        var model = new CEODashboardViewModel
        {
            TotalEmployees = users.Count(),
            TotalManagers = users.Count(u => u.Roles.Contains("Manager")),
            TotalWorkers = users.Count(u => u.Roles.Contains("Worker") && !u.Roles.Contains("Manager")),
            UnassignedWorkers = users.Count(u => u.Roles.Contains("Worker") && u.ManagerId == null),
            PendingTimeOffRequests = 0,
            RecentShifts = 0
        };

        // Get pending time off requests
        var currentUserId = GetCurrentUserId();
        var pendingRequests = await _timeOffService.GetPendingRequestsByManagerAsync(currentUserId);
        model.PendingTimeOffRequests = pendingRequests.Count();

        // Get today's shifts count
        foreach (var user in users.Take(50))
        {
            var shifts = await _shiftService.GetShiftsByUserAsync(user.Id, currentUserId);
            model.RecentShifts += shifts.Count(s => s.ShiftDate.Date == today);
        }

        return View(model);
    }

    // GET: CEO/Employees
    public async Task<IActionResult> Employees()
    {
        var users = await _userService.GetAllUsersAsync();
        
        // Group by department
        var byDepartment = users
            .Where(u => !string.IsNullOrEmpty(u.Department))
            .GroupBy(u => u.Department!)
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.ByDepartment = byDepartment;
        ViewBag.Unassigned = users.Where(u => u.ManagerId == null && u.Roles.Contains("Worker")).ToList();

        return View(users);
    }

    // GET: CEO/CreateEmployee
    public IActionResult CreateEmployee()
    {
        return View();
    }

    // POST: CEO/CreateEmployee
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _userService.RegisterAsync(model, ipAddress);

        if (result.Success)
        {
            TempData["Success"] = $"Employee {model.Email} created successfully.";
            return RedirectToAction(nameof(Employees));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // GET: CEO/EditEmployee/5
    public async Task<IActionResult> EditEmployee(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new UpdateProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            Position = user.Position
        };

        ViewBag.UserId = id;
        ViewBag.UserRoles = user.Roles;
        ViewBag.CurrentManagerId = user.ManagerId;

        // Get all managers for assignment
        var users = await _userService.GetAllUsersAsync();
        ViewBag.Managers = users.Where(u => u.Roles.Contains("Manager") || u.Roles.Contains("CEO"));

        return View(model);
    }

    // POST: CEO/EditEmployee/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(int id, UpdateProfileDto model, string? role, int? managerId)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.UpdateProfileAsync(id, model);

        if (result.Success)
        {
            // Update role if specified
            if (!string.IsNullOrEmpty(role))
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            // Update manager assignment
            if (managerId.HasValue)
            {
                await _userService.AssignManagerAsync(id, managerId.Value);
            }

            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction(nameof(Employees));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // POST: CEO/DeleteEmployee/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        // Don't allow deleting CEO
        if (await _userManager.IsInRoleAsync(user, "CEO"))
        {
            TempData["Error"] = "Cannot delete CEO account.";
            return RedirectToAction(nameof(Employees));
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            TempData["Success"] = "Employee deleted successfully.";
        }
        else
        {
            TempData["Error"] = "Failed to delete employee.";
        }

        return RedirectToAction(nameof(Employees));
    }

    // GET: CEO/Departments
    public async Task<IActionResult> Departments()
    {
        var users = await _userService.GetAllUsersAsync();
        
        var departments = users
            .Where(u => !string.IsNullOrEmpty(u.Department))
            .GroupBy(u => u.Department!)
            .Select(g => new DepartmentViewModel
            {
                Name = g.Key,
                EmployeeCount = g.Count(),
                ManagerCount = g.Count(u => u.Roles.Contains("Manager")),
                WorkersWithoutManager = g.Count(u => u.Roles.Contains("Worker") && u.ManagerId == null)
            })
            .OrderBy(d => d.Name)
            .ToList();

        return View(departments);
    }

    // GET: CEO/Reports
    public async Task<IActionResult> Reports()
    {
        var users = await _userService.GetAllUsersAsync();
        var currentUserId = GetCurrentUserId();

        var model = new ReportsViewModel
        {
            TotalEmployees = users.Count(),
            TotalDepartments = users.Where(u => !string.IsNullOrEmpty(u.Department)).Select(u => u.Department).Distinct().Count(),
            ReportDate = DateTime.Today
        };

        // Time off statistics
        var allRequests = new List<TimeOffRequestDto>();
        foreach (var user in users.Take(100))
        {
            var requests = await _timeOffService.GetRequestsByUserAsync(user.Id, currentUserId);
            allRequests.AddRange(requests);
        }

        model.TotalTimeOffRequests = allRequests.Count;
        model.PendingRequests = allRequests.Count(r => r.Status == "Pending");
        model.ApprovedRequests = allRequests.Count(r => r.Status == "Approved");
        model.RejectedRequests = allRequests.Count(r => r.Status == "Rejected");

        return View(model);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}