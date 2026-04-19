using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Services.Interfaces;

namespace StaffShift.Web.Controllers;

/// <summary>
/// Controller for forum operations - work information exchange
/// </summary>
[Authorize]
public class ForumController : Controller
{
    private readonly IForumService _forumService;
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;

    public ForumController(IForumService forumService, IUserService userService, UserManager<User> userManager)
    {
        _forumService = forumService;
        _userService = userService;
        _userManager = userManager;
    }

    // GET: Forum
    public async Task<IActionResult> Index(string? department, int page = 1)
    {
        var userId = GetCurrentUserId();
        IEnumerable<ForumPostDto> posts;

        if (!string.IsNullOrEmpty(department))
        {
            posts = await _forumService.GetPostsByDepartmentAsync(department, userId);
            ViewBag.CurrentDepartment = department;
        }
        else
        {
            posts = await _forumService.GetRecentPostsAsync(50, userId);
        }

        // Get user's department for filter
        var user = await _userService.GetUserByIdAsync(userId);
        ViewBag.UserDepartment = user?.Department;

        // Check if user can manage posts (CEO or Manager)
        ViewBag.CanManage = User.IsInRole("CEO") || User.IsInRole("Manager");

        return View(posts);
    }

    // GET: Forum/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        var post = await _forumService.GetPostByIdAsync(id, userId);

        if (post == null)
        {
            return NotFound();
        }

        // Get comments for the post
        var comments = await _forumService.GetCommentsByPostAsync(id, userId);
        ViewBag.Comments = comments;

        // Check if user can edit/delete
        ViewBag.CanEdit = post.AuthorId == userId;
        ViewBag.CanManage = User.IsInRole("CEO") || User.IsInRole("Manager");

        return View(post);
    }

    // GET: Forum/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Forum/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateForumPostDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var result = await _forumService.CreatePostAsync(model, userId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Post!.Id });
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // GET: Forum/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        var post = await _forumService.GetPostByIdAsync(id, userId);

        if (post == null)
        {
            return NotFound();
        }

        // Only author can edit
        if (post.AuthorId != userId)
        {
            return Forbid();
        }

        var model = new UpdateForumPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Department = post.Department
        };

        return View(model);
    }

    // POST: Forum/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateForumPostDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var result = await _forumService.UpdatePostAsync(model, userId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        ModelState.AddModelError("", result.Message);
        return View(model);
    }

    // POST: Forum/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("CEO") || User.IsInRole("Manager");

        var result = await _forumService.DeletePostAsync(id, userId, isAdmin);

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

    // POST: Forum/Pin/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Pin(int id)
    {
        var userId = GetCurrentUserId();
        var isManager = User.IsInRole("CEO") || User.IsInRole("Manager");

        var result = await _forumService.TogglePinAsync(id, userId, isManager);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Forum/Lock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "CEO,Manager")]
    public async Task<IActionResult> Lock(int id)
    {
        var userId = GetCurrentUserId();
        var isManager = User.IsInRole("CEO") || User.IsInRole("Manager");

        var result = await _forumService.ToggleLockAsync(id, userId, isManager);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Forum/Comment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(CreateForumCommentDto model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Details), new { id = model.PostId });
        }

        var userId = GetCurrentUserId();
        var result = await _forumService.AddCommentAsync(model, userId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.PostId });
    }

    // POST: Forum/DeleteComment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int postId)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("CEO") || User.IsInRole("Manager");

        var result = await _forumService.DeleteCommentAsync(id, userId, isAdmin);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = postId });
    }

    // GET: Forum/MyPosts
    public async Task<IActionResult> MyPosts()
    {
        var userId = GetCurrentUserId();
        var posts = await _forumService.GetPostsByUserAsync(userId, userId);
        return View(posts);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}