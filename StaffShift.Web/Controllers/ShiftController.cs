using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Controller for shift management
/// </summary>
[Authorize]
public class ShiftController : Controller
{
    private readonly IShiftService _shiftService;
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;

    public ShiftController(IShiftService shiftService, IUserService _userService, UserManager<User> userManager)
    {
        _shiftService = shiftService;
        this._userService = _userService;
        _userManager = userManager;
    }

    // GET: Shift
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");
        var isManager = User.IsInRole("Manager");

        if (isCEO)
        {
            // CEO sees all shifts for current week
            var startDate = DateTime.UtcNow.StartOfWeek(DayOfWeek.Monday);
            var endDate = startDate.AddDays(7);
            var allShifts = await _shiftService.GetShiftsByDateRangeAsync(userId, startDate, endDate, userId);
            return View(allShifts);
        }
        else if (isManager)
        {
            // Manager sees team shifts
            var startDate = DateTime.UtcNow.StartOfWeek(DayOfWeek.Monday);
            var endDate = startDate.AddDays(7);
            var teamShifts = await _shiftService.GetTeamShiftsAsync(userId, startDate, endDate, userId);
            var myShifts = await _shiftService.GetShiftsByUserAsync(userId, userId);
            ViewBag.TeamShifts = teamShifts;
            return View(myShifts);
        }
        else
        {
            // Worker sees only their shifts
            var shifts = await _shiftService.GetShiftsByUserAsync(userId, userId);
            return View(shifts);
        }
    }

    // GET: Shift/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var shift = await _shiftService.GetShiftByIdAsync(id, GetCurrentUserId());
        if (shift == null)
        {
            return NotFound();
        }

        return View(shift);
    }

    // GET: Shift/Create
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Create(int? userId = null)
    {
        var currentUserId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");
        var isManager = User.IsInRole("Manager");

        if (isCEO)
        {
            // CEO can assign to anyone
            ViewBag.Users = await _userService.GetAllWorkersAsync(currentUserId);
        }
        else if (isManager)
        {
            // Manager can assign to subordinates
            ViewBag.Users = await _userService.GetSubordinatesAsync(currentUserId, currentUserId);
        }

        if (userId.HasValue)
        {
            ViewBag.SelectedUserId = userId.Value;
        }

        return View();
    }

    // POST: Shift/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Create(CreateShiftDto model)
    {
        if (!ModelState.IsValid)
        {
            var currentUserId = GetCurrentUserId();
            if (User.IsInRole("CEO"))
            {
                ViewBag.Users = await _userService.GetAllWorkersAsync(currentUserId);
            }
            else
            {
                ViewBag.Users = await _userService.GetSubordinatesAsync(currentUserId, currentUserId);
            }
            return View(model);
        }

        var result = await _shiftService.CreateShiftAsync(model, GetCurrentUserId());
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // GET: Shift/Edit/5
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var shift = await _shiftService.GetShiftByIdAsync(id, GetCurrentUserId());
        if (shift == null)
        {
            return NotFound();
        }

        var model = new UpdateShiftDto
        {
            Id = shift.Id,
            ShiftDate = shift.ShiftDate,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Status = shift.Status,
            Notes = shift.Notes
        };

        return View(model);
    }

    // POST: Shift/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Edit(UpdateShiftDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _shiftService.UpdateShiftAsync(model, GetCurrentUserId());
        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // POST: Shift/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _shiftService.DeleteShiftAsync(id);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Shift/ClockIn/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn(int id)
    {
        var result = await _shiftService.ClockInAsync(id);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Shift/ClockOut/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut(int id)
    {
        var result = await _shiftService.ClockOutAsync(id);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}