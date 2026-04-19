using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Controller for time off request management
/// </summary>
[Authorize]
public class TimeOffController : Controller
{
    private readonly ITimeOffService _timeOffService;
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;

    public TimeOffController(ITimeOffService timeOffService, IUserService userService, UserManager<User> userManager)
    {
        _timeOffService = timeOffService;
        _userService = userService;
        _userManager = userManager;
    }

    // GET: TimeOff
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var requests = await _timeOffService.GetRequestsByUserAsync(userId, userId);
        
        var isCEO = User.IsInRole("CEO");
        var isManager = User.IsInRole("Manager");

        if (isCEO || isManager)
        {
            var pendingRequests = await _timeOffService.GetPendingRequestsByManagerAsync(userId);
            ViewBag.PendingRequests = pendingRequests;
        }

        // Get time off summary
        var summary = await _timeOffService.GetTimeOffSummaryAsync(userId, DateTime.UtcNow.Year);
        ViewBag.TimeOffSummary = summary;

        return View(requests);
    }

    // GET: TimeOff/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var request = await _timeOffService.GetRequestByIdAsync(id, GetCurrentUserId());
        if (request == null)
        {
            return NotFound();
        }

        return View(request);
    }

    // GET: TimeOff/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TimeOff/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTimeOffRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var result = await _timeOffService.CreateRequestAsync(model, userId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // GET: TimeOff/Approve/5
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Review(int id)
    {
        var request = await _timeOffService.GetRequestByIdAsync(id, GetCurrentUserId());
        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Pending")
        {
            TempData["Error"] = "This request has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        return View(request);
    }

    // POST: TimeOff/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Review(ReviewTimeOffRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            var request = await _timeOffService.GetRequestByIdAsync(model.RequestId, GetCurrentUserId());
            return View(request);
        }

        var userId = GetCurrentUserId();
        var result = await _timeOffService.ReviewRequestAsync(model, userId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        var req = await _timeOffService.GetRequestByIdAsync(model.RequestId, userId);
        return View(req);
    }

    // POST: TimeOff/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _timeOffService.CancelRequestAsync(id, userId);

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