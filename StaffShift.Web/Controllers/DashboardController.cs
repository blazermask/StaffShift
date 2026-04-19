using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Dashboard controller for user's main landing page
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IShiftService _shiftService;
    private readonly ITimeOffService _timeOffService;
    private readonly IForumService _forumService;
    private readonly UserManager<User> _userManager;

    public DashboardController(IUserService userService, IShiftService shiftService,
        ITimeOffService timeOffService, IForumService forumService, UserManager<User> userManager)
    {
        _userService = userService;
        _shiftService = shiftService;
        _timeOffService = timeOffService;
        _forumService = forumService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
            return RedirectToAction("Login", "Account");

        var upcomingShifts = await _shiftService.GetUpcomingShiftsAsync(userId, userId);
        ViewBag.UpcomingShifts = upcomingShifts.Take(5);

        var timeOffRequests = await _timeOffService.GetRequestsByUserAsync(userId, userId);
        ViewBag.PendingRequests = timeOffRequests.Count(r => r.Status == "Pending");

        var isCEO = User.IsInRole("CEO");
        var isManager = User.IsInRole("Manager");
        ViewBag.IsCEO = isCEO;
        ViewBag.IsManager = isManager;

        if (isCEO || isManager)
        {
            var pendingApprovals = await _timeOffService.GetPendingRequestsByManagerAsync(userId);
            ViewBag.PendingApprovals = pendingApprovals;
        }

        var recentPosts = await _forumService.GetRecentPostsAsync(5, userId);
        ViewBag.RecentPosts = recentPosts;

        var weeklyHours = await _shiftService.GetWeeklyHoursAsync(userId);
        ViewBag.WeeklyHours = weeklyHours;

        var timeOffSummary = await _timeOffService.GetTimeOffSummaryAsync(userId, DateTime.UtcNow.Year);
        ViewBag.TimeOffSummary = timeOffSummary;

        return View(user);
    }

    // GET: Dashboard/Calendar
    public async Task<IActionResult> Calendar()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return RedirectToAction("Login", "Account");

        ViewBag.IsCEO = User.IsInRole("CEO");
        ViewBag.IsManager = User.IsInRole("Manager");
        ViewBag.UserName = user.FirstName ?? user.Username;

        return View();
    }

    // GET: Dashboard/CalendarEvents - returns JSON for FullCalendar
    [HttpGet]
    public async Task<IActionResult> CalendarEvents(DateTime? start, DateTime? end)
    {
        var userId = GetCurrentUserId();
        var isCEO = User.IsInRole("CEO");
        var isManager = User.IsInRole("Manager");

        var fromDate = start ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = end ?? DateTime.UtcNow.AddMonths(2);

        var events = new List<object>();

        // Add the user's own shifts
        var shifts = await _shiftService.GetShiftsByUserAsync(userId, userId);
        foreach (var shift in shifts.Where(s => s.ShiftDate >= fromDate && s.ShiftDate <= toDate))
        {
            var color = shift.Status switch
            {
                "Completed" => "#38a169",
                "Cancelled" => "#e53e3e",
                "InProgress" => "#dd6b20",
                _ => "#3182ce"
            };
            events.Add(new
            {
                id = $"shift-{shift.Id}",
                title = $"🕐 {shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}",
                start = shift.ShiftDate.ToString("yyyy-MM-dd") + "T" + shift.StartTime.ToString(@"hh\:mm\:ss"),
                end = shift.ShiftDate.ToString("yyyy-MM-dd") + "T" + shift.EndTime.ToString(@"hh\:mm\:ss"),
                color,
                textColor = "#fff",
                extendedProps = new { type = "shift", status = shift.Status, notes = shift.Notes ?? "" }
            });
        }

        // Add time off requests
        var timeOffRequests = await _timeOffService.GetRequestsByUserAsync(userId, userId);
        foreach (var req in timeOffRequests.Where(r => r.StartDate >= fromDate && r.StartDate <= toDate))
        {
            var color = req.Status switch
            {
                "Approved" => "#805ad5",
                "Rejected" => "#e53e3e",
                _ => "#d69e2e"
            };
            var icon = req.RequestType switch
            {
                "Vacation" => req.IsPaid ? "🏖️" : "⛱️",
                "Sick" => "🏥",
                "Personal" => "👤",
                _ => "📅"
            };
            var paidLabel = req.RequestType == "Vacation" ? (req.IsPaid ? " (Paid)" : " (Unpaid)") : "";
            events.Add(new
            {
                id = $"timeoff-{req.Id}",
                title = $"{icon} {req.RequestType}{paidLabel}",
                start = req.StartDate.ToString("yyyy-MM-dd"),
                end = req.EndDate.AddDays(1).ToString("yyyy-MM-dd"),
                color,
                textColor = "#fff",
                allDay = true,
                extendedProps = new { type = "timeoff", status = req.Status, requestType = req.RequestType, isPaid = req.IsPaid }
            });
        }

        // For Managers/CEO: also show team shifts and time off
        if (isManager || isCEO)
        {
            IEnumerable<StaffShift.Core.DTOs.UserDto> team;
            if (isCEO)
                team = await _userService.GetAllUsersAsync(userId);
            else
                team = await _userService.GetSubordinatesAsync(userId, userId);

            foreach (var member in team.Where(m => m.Id != userId))
            {
                var memberShifts = await _shiftService.GetShiftsByUserAsync(member.Id, userId);
                foreach (var shift in memberShifts.Where(s => s.ShiftDate >= fromDate && s.ShiftDate <= toDate))
                {
                    events.Add(new
                    {
                        id = $"team-shift-{shift.Id}",
                        title = $"👤 {member.FirstName ?? member.Username}: {shift.StartTime:hh\\:mm}-{shift.EndTime:hh\\:mm}",
                        start = shift.ShiftDate.ToString("yyyy-MM-dd") + "T" + shift.StartTime.ToString(@"hh\:mm\:ss"),
                        end = shift.ShiftDate.ToString("yyyy-MM-dd") + "T" + shift.EndTime.ToString(@"hh\:mm\:ss"),
                        color = "#718096",
                        textColor = "#fff",
                        extendedProps = new { type = "team-shift", status = shift.Status, memberName = member.FirstName ?? member.Username }
                    });
                }

                var memberTimeOff = await _timeOffService.GetRequestsByUserAsync(member.Id, userId);
                foreach (var req in memberTimeOff.Where(r => r.StartDate >= fromDate && r.StartDate <= toDate && r.Status == "Approved"))
                {
                    var paidLabel = req.RequestType == "Vacation" ? (req.IsPaid ? " (Paid)" : " (Unpaid)") : "";
                    events.Add(new
                    {
                        id = $"team-timeoff-{req.Id}",
                        title = $"🏖️ {member.FirstName ?? member.Username}: {req.RequestType}{paidLabel}",
                        start = req.StartDate.ToString("yyyy-MM-dd"),
                        end = req.EndDate.AddDays(1).ToString("yyyy-MM-dd"),
                        color = "#b794f4",
                        textColor = "#fff",
                        allDay = true,
                        extendedProps = new { type = "team-timeoff", status = req.Status, memberName = member.FirstName ?? member.Username }
                    });
                }
            }
        }

        return Json(events);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}