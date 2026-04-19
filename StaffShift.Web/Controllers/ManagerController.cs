using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Controller for manager team management operations
/// </summary>
[Authorize(Roles = "CEO,Manager")]
public class ManagerController : Controller
{
    private readonly IUserService _userService;
    private readonly IShiftService _shiftService;
    private readonly ITimeOffService _timeOffService;
    private readonly UserManager<User> _userManager;

    public ManagerController(IUserService userService, IShiftService shiftService, ITimeOffService timeOffService, UserManager<User> userManager)
    {
        _userService = userService;
        _shiftService = shiftService;
        _timeOffService = timeOffService;
        _userManager = userManager;
    }

    // GET: Manager/Team
    public async Task<IActionResult> Team()
    {
        var userId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");

        // CEO sees all users, Manager sees only their subordinates
        var team = isCEO 
            ? await _userService.GetAllUsersAsync() 
            : await _userService.GetSubordinatesAsync(userId);

        // Get shift summaries for each team member
        var teamWithStats = new List<TeamMemberViewModel>();
        foreach (var member in team)
        {
            var shifts = await _shiftService.GetShiftsByUserAsync(member.Id, userId);
            var pendingRequests = (await _timeOffService.GetRequestsByUserAsync(member.Id, userId))
                .Count(r => r.Status == "Pending");

            teamWithStats.Add(new TeamMemberViewModel
            {
                User = member,
                TotalShifts = shifts.Count(),
                PendingTimeOffRequests = pendingRequests
            });
        }

        return View(teamWithStats);
    }

    // GET: Manager/Member/5
    public async Task<IActionResult> Member(int id)
    {
        var currentUserId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if manager has access to this user
        if (!isCEO && user.ManagerId != currentUserId)
        {
            return Forbid();
        }

        // Get user's shifts
        var shifts = await _shiftService.GetShiftsByUserAsync(id, currentUserId);

        // Get user's time off requests
        var timeOffRequests = await _timeOffService.GetRequestsByUserAsync(id, currentUserId);

        // Get time off summary
        var timeOffSummary = await _timeOffService.GetTimeOffSummaryAsync(id, DateTime.UtcNow.Year);

        var model = new MemberDetailViewModel
        {
            User = user,
            Shifts = shifts,
            TimeOffRequests = timeOffRequests,
            TimeOffSummary = timeOffSummary
        };

        return View(model);
    }

    // GET: Manager/AssignManager
    [Authorize(Roles = "CEO")]
    public async Task<IActionResult> AssignManager()
    {
        var users = await _userService.GetAllUsersAsync();
        var managers = users.Where(u => u.Roles.Contains("Manager") || u.Roles.Contains("CEO")).ToList();
        var workers = users.Where(u => u.Roles.Contains("Worker") && !u.Roles.Contains("Manager")).ToList();

        ViewBag.Managers = managers;
        ViewBag.Workers = workers;

        return View();
    }

    // POST: Manager/AssignManager
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO")]
    public async Task<IActionResult> AssignManager(AssignManagerDto model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Managers = users.Where(u => u.Roles.Contains("Manager") || u.Roles.Contains("CEO"));
            ViewBag.Workers = users.Where(u => u.Roles.Contains("Worker"));
            return View(model);
        }

        var result = await _userService.AssignManagerAsync(model.WorkerId, model.ManagerId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Team));
        }

        ModelState.AddModelError("", result.Message);
        var allUsers = await _userService.GetAllUsersAsync();
        ViewBag.Managers = allUsers.Where(u => u.Roles.Contains("Manager") || u.Roles.Contains("CEO"));
        ViewBag.Workers = allUsers.Where(u => u.Roles.Contains("Worker"));
        return View(model);
    }

    // GET: Manager/Shifts
    public async Task<IActionResult> Shifts(DateTime? date)
    {
        var userId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");

        var shiftDate = date ?? DateTime.Today;
        ViewBag.SelectedDate = shiftDate;

        // Get team members
        var team = isCEO 
            ? await _userService.GetAllUsersAsync() 
            : await _userService.GetSubordinatesAsync(userId);

        // Get all shifts for the date
        var teamIds = team.Select(t => t.Id).ToList();
        var allShifts = new List<ShiftDto>();

        foreach (var teamId in teamIds)
        {
            var userShifts = await _shiftService.GetShiftsByUserAsync(teamId, userId);
            allShifts.AddRange(userShifts.Where(s => s.ShiftDate.Date == shiftDate.Date));
        }

        return View(allShifts);
    }

    // GET: Manager/TimeOffRequests
    public async Task<IActionResult> TimeOffRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await _timeOffService.GetPendingRequestsByManagerAsync(userId);
        return View(requests);
    }

    // GET: Manager/AssignShifts
    public async Task<IActionResult> AssignShifts()
    {
        var userId = GetCurrentUserId();
        var team = await _userService.GetSubordinatesAsync(userId);
        
        ViewBag.Team = team;
        return View();
    }

    // POST: Manager/AssignShift
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignShift(CreateShiftDto model)
    {
        if (!ModelState.IsValid)
        {
            var userId = GetCurrentUserId();
            var team = await _userService.GetSubordinatesAsync(userId);
            ViewBag.Team = team;
            return View("AssignShifts", model);
        }

        var result = await _shiftService.CreateShiftAsync(model, GetCurrentUserId());

        if (result.Success)
        {
            TempData["Success"] = "Shift assigned successfully!";
            return RedirectToAction(nameof(AssignShifts));
        }

        ModelState.AddModelError("", result.Message);
        ViewBag.Team = await _userService.GetSubordinatesAsync(GetCurrentUserId());
        return View("AssignShifts", model);
    }

    // GET: Manager/TeamShifts
    public async Task<IActionResult> TeamShifts(DateTime? date)
    {
        var userId = GetCurrentUserId();
        var shiftDate = date ?? DateTime.Today;
        ViewBag.SelectedDate = shiftDate;

        var team = await _userService.GetSubordinatesAsync(userId);
        var teamIds = team.Select(t => t.Id).ToList();
        var allShifts = new List<ShiftDto>();

        foreach (var teamId in teamIds)
        {
            var userShifts = await _shiftService.GetShiftsByUserAsync(teamId, userId);
            allShifts.AddRange(userShifts.Where(s => s.ShiftDate.Date == shiftDate.Date));
        }

        return View(allShifts);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}