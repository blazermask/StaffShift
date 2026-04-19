using Microsoft.EntityFrameworkCore;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Repositories;
using StaffShift.Services.Services;

namespace StaffShift.Tests;

/// <summary>
/// XUnit tests for ForumService
/// </summary>
public class ForumServiceTests : IDisposable
{
    private readonly StaffShiftDbContext _context;
    private readonly ForumService _forumService;
    private readonly ForumRepository _forumRepository;
    private readonly ForumCommentRepository _forumCommentRepository;
    private readonly UserRepository _userRepository;

    public ForumServiceTests()
    {
        var options = new DbContextOptionsBuilder<StaffShiftDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StaffShiftDbContext(options);
        _forumRepository = new ForumRepository(_context);
        _forumCommentRepository = new ForumCommentRepository(_context);
        _userRepository = new UserRepository(_context);
        _forumService = new ForumService(_forumRepository, _forumCommentRepository, _userRepository);

        SeedData();
    }

    private void SeedData()
    {
        _context.Users.AddRange(
            new User { Id = 1, UserName = "ceo", Email = "ceo@test.com", IsActive = true, HireDate = DateTime.UtcNow },
            new User { Id = 2, UserName = "manager", Email = "manager@test.com", IsActive = true, HireDate = DateTime.UtcNow },
            new User { Id = 3, UserName = "worker", Email = "worker@test.com", IsActive = true, ManagerId = 2, HireDate = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreatePostAsync_ValidPost_ReturnsSuccess()
    {
        var model = new CreateForumPostDto
        {
            Title = "Team Meeting Tomorrow",
            Content = "Please join the team meeting at 10am in the conference room. We will be discussing Q2 targets.",
            Category = "Announcement"
        };

        var result = await _forumService.CreatePostAsync(model, 2);

        Assert.True(result.Success);
        Assert.NotNull(result.Post);
        Assert.Equal("Team Meeting Tomorrow", result.Post.Title);
        Assert.Equal(2, result.Post.UserId);
    }

    [Fact]
    public async Task CreatePostAsync_UserNotFound_ReturnsFail()
    {
        var model = new CreateForumPostDto
        {
            Title = "Test Post",
            Content = "This is a test post content that is long enough.",
            Category = "Discussion"
        };

        var result = await _forumService.CreatePostAsync(model, 999);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPostByIdAsync_ExistingPost_ReturnsDto()
    {
        var post = new ForumPost
        {
            UserId = 2,
            Title = "Test Post",
            Content = "Test content for the post.",
            Category = "Discussion",
            CreatedAt = DateTime.UtcNow
        };
        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _forumService.GetPostByIdAsync(post.Id, 2);

        Assert.NotNull(result);
        Assert.Equal("Test Post", result.Title);
        Assert.Equal(2, result.UserId);
    }

    [Fact]
    public async Task GetPostByIdAsync_NonExistentPost_ReturnsNull()
    {
        var result = await _forumService.GetPostByIdAsync(9999, 1);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePostAsync_OwnPost_ReturnsSuccess()
    {
        var post = new ForumPost
        {
            UserId = 3,
            Title = "Original Title",
            Content = "Original content for the forum post.",
            Category = "Discussion",
            CreatedAt = DateTime.UtcNow
        };
        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateForumPostDto
        {
            Id = post.Id,
            Title = "Updated Title",
            Content = "Updated content for the forum post."
        };

        var result = await _forumService.UpdatePostAsync(updateModel, 3);

        Assert.True(result.Success);
        Assert.Equal("Updated Title", result.Post!.Title);
    }

    [Fact]
    public async Task UpdatePostAsync_OtherUsersPost_ReturnsFail()
    {
        var post = new ForumPost
        {
            UserId = 3,
            Title = "Worker Post",
            Content = "Content written by worker.",
            Category = "Discussion",
            CreatedAt = DateTime.UtcNow
        };
        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var updateModel = new UpdateForumPostDto
        {
            Id = post.Id,
            Title = "Hacked Title",
            Content = "This should not be allowed."
        };

        // User 2 (manager) tries to update user 3 (worker) post
        var result = await _forumService.UpdatePostAsync(updateModel, 2);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeletePostAsync_OwnPost_ReturnsSuccess()
    {
        var post = new ForumPost
        {
            UserId = 3,
            Title = "Post to Delete",
            Content = "This post will be deleted.",
            Category = "Discussion",
            CreatedAt = DateTime.UtcNow
        };
        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var result = await _forumService.DeletePostAsync(post.Id, 3, isAdmin: false);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AddCommentAsync_ValidComment_ReturnsSuccess()
    {
        var post = new ForumPost
        {
            UserId = 2,
            Title = "Post with Comment",
            Content = "Content of the post that will receive a comment.",
            Category = "Discussion",
            CreatedAt = DateTime.UtcNow
        };
        _context.ForumPosts.Add(post);
        await _context.SaveChangesAsync();

        var commentModel = new CreateForumCommentDto
        {
            ForumPostId = post.Id,
            Content = "Great post! Thanks for sharing."
        };

        var result = await _forumService.AddCommentAsync(commentModel, 3);

        Assert.True(result.Success);
        Assert.NotNull(result.Comment);
        Assert.Equal(3, result.Comment.UserId);
    }

    [Fact]
    public async Task GetRecentPostsAsync_ReturnsMostRecentFirst()
    {
        await _context.ForumPosts.AddRangeAsync(
            new ForumPost { UserId = 2, Title = "Old Post", Content = "Old content here.", Category = "Discussion", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new ForumPost { UserId = 2, Title = "New Post", Content = "New content here.", Category = "Discussion", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new ForumPost { UserId = 3, Title = "Newest Post", Content = "Newest content here.", Category = "Announcement", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = (await _forumService.GetRecentPostsAsync(2, 1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].CreatedAt >= result[1].CreatedAt);
    }

    [Fact]
    public async Task GetPostsByUserAsync_ReturnsOnlyUserPosts()
    {
        await _context.ForumPosts.AddRangeAsync(
            new ForumPost { UserId = 3, Title = "Worker Post 1", Content = "Content by worker.", Category = "Discussion", CreatedAt = DateTime.UtcNow },
            new ForumPost { UserId = 2, Title = "Manager Post", Content = "Content by manager.", Category = "Discussion", CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _forumService.GetPostsByUserAsync(3, 3);

        Assert.All(result, p => Assert.Equal(3, p.UserId));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}