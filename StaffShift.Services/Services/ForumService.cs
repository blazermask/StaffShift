using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Repository.Interfaces;
using StaffShift.Services.Interfaces;

namespace StaffShift.Services.Services;

/// <summary>
/// Service implementation for forum operations
/// </summary>
public class ForumService : IForumService
{
    private readonly IForumRepository _forumRepository;
    private readonly IForumCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public ForumService(IForumRepository forumRepository, IForumCommentRepository commentRepository, IUserRepository userRepository)
    {
        _forumRepository = forumRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
    }

    public async Task<ForumPostDto?> GetPostByIdAsync(int postId, int? currentUserId = null)
    {
        var post = await _forumRepository.GetPostWithCommentsAsync(postId);
        if (post == null) return null;

        return await MapToForumPostDto(post, currentUserId);
    }

    public async Task<IEnumerable<ForumPostDto>> GetRecentPostsAsync(int count = 20, int? currentUserId = null)
    {
        var posts = await _forumRepository.GetRecentPostsAsync(count);
        var postDtos = new List<ForumPostDto>();
        foreach (var post in posts)
        {
            postDtos.Add(await MapToForumPostDto(post, currentUserId));
        }
        return postDtos;
    }

    public async Task<IEnumerable<ForumPostDto>> GetPostsByUserAsync(int userId, int? currentUserId = null)
    {
        var posts = await _forumRepository.GetPostsByUserAsync(userId);
        var postDtos = new List<ForumPostDto>();
        foreach (var post in posts)
        {
            postDtos.Add(await MapToForumPostDto(post, currentUserId));
        }
        return postDtos;
    }

    public async Task<IEnumerable<ForumPostDto>> GetPostsByDepartmentAsync(string? department, int? currentUserId = null)
    {
        var posts = await _forumRepository.GetPostsByDepartmentAsync(department);
        var postDtos = new List<ForumPostDto>();
        foreach (var post in posts)
        {
            postDtos.Add(await MapToForumPostDto(post, currentUserId));
        }
        return postDtos;
    }

    public async Task<(bool Success, string Message, ForumPostDto? Post)> CreatePostAsync(CreateForumPostDto model, int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        var post = new ForumPost
        {
            UserId = userId,
            Title = model.Title,
            Content = model.Content,
            Category = model.Category,
            TargetDepartment = model.TargetDepartment,
            CreatedAt = DateTime.UtcNow
        };

        await _forumRepository.AddAsync(post);
        await _forumRepository.SaveChangesAsync();

        var postDto = await MapToForumPostDto(post, userId);
        return (true, "Post created successfully!", postDto);
    }

    public async Task<(bool Success, string Message, ForumPostDto? Post)> UpdatePostAsync(UpdateForumPostDto model, int userId)
    {
        var post = await _forumRepository.GetByIdAsync(model.Id);
        if (post == null)
        {
            return (false, "Post not found.", null);
        }

        if (post.UserId != userId)
        {
            return (false, "You can only edit your own posts.", null);
        }

        if (model.Title != null) post.Title = model.Title;
        if (model.Content != null) post.Content = model.Content;
        if (model.Category != null) post.Category = model.Category;
        if (model.TargetDepartment != null) post.TargetDepartment = model.TargetDepartment;

        post.UpdatedAt = DateTime.UtcNow;
        post.LastEditedBy = userId;

        await _forumRepository.UpdateAsync(post);
        await _forumRepository.SaveChangesAsync();

        var postDto = await MapToForumPostDto(post, userId);
        return (true, "Post updated successfully!", postDto);
    }

    public async Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId, bool isAdmin)
    {
        var post = await _forumRepository.GetByIdAsync(postId);
        if (post == null)
        {
            return (false, "Post not found.");
        }

        if (post.UserId != userId && !isAdmin)
        {
            return (false, "You can only delete your own posts.");
        }

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _forumRepository.UpdateAsync(post);
        await _forumRepository.SaveChangesAsync();

        return (true, "Post deleted successfully!");
    }

    public async Task<(bool Success, string Message, ForumPostDto? Post)> TogglePinAsync(int postId, int userId, bool isManager)
    {
        if (!isManager)
        {
            return (false, "Only managers can pin posts.", null);
        }

        var post = await _forumRepository.GetByIdAsync(postId);
        if (post == null)
        {
            return (false, "Post not found.", null);
        }

        post.IsPinned = !post.IsPinned;
        post.UpdatedAt = DateTime.UtcNow;
        post.LastEditedBy = userId;

        await _forumRepository.UpdateAsync(post);
        await _forumRepository.SaveChangesAsync();

        var postDto = await MapToForumPostDto(post, userId);
        return (true, post.IsPinned ? "Post pinned successfully!" : "Post unpinned successfully!", postDto);
    }

    public async Task<(bool Success, string Message, ForumPostDto? Post)> ToggleLockAsync(int postId, int userId, bool isManager)
    {
        if (!isManager)
        {
            return (false, "Only managers can lock posts.", null);
        }

        var post = await _forumRepository.GetByIdAsync(postId);
        if (post == null)
        {
            return (false, "Post not found.", null);
        }

        post.IsLocked = !post.IsLocked;
        post.UpdatedAt = DateTime.UtcNow;
        post.LastEditedBy = userId;

        await _forumRepository.UpdateAsync(post);
        await _forumRepository.SaveChangesAsync();

        var postDto = await MapToForumPostDto(post, userId);
        return (true, post.IsLocked ? "Post locked successfully!" : "Post unlocked successfully!", postDto);
    }

    public async Task<(bool Success, string Message, ForumCommentDto? Comment)> AddCommentAsync(CreateForumCommentDto model, int userId)
    {
        var post = await _forumRepository.GetByIdAsync(model.ForumPostId);
        if (post == null)
        {
            return (false, "Post not found.", null);
        }

        if (post.IsLocked)
        {
            return (false, "This post is locked and cannot receive new comments.", null);
        }

        var comment = new ForumComment
        {
            ForumPostId = model.ForumPostId,
            UserId = userId,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment);
        await _commentRepository.SaveChangesAsync();

        var commentDto = await MapToForumCommentDto(comment, userId);
        return (true, "Comment added successfully!", commentDto);
    }

    public async Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId, bool isAdmin)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return (false, "Comment not found.");
        }

        if (comment.UserId != userId && !isAdmin)
        {
            return (false, "You can only delete your own comments.");
        }

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment);
        await _commentRepository.SaveChangesAsync();

        return (true, "Comment deleted successfully!");
    }

    public async Task<IEnumerable<ForumCommentDto>> GetCommentsByPostAsync(int postId, int? currentUserId = null)
    {
        var comments = await _commentRepository.GetCommentsByPostAsync(postId);
        var commentDtos = new List<ForumCommentDto>();
        foreach (var comment in comments)
        {
            commentDtos.Add(await MapToForumCommentDto(comment, currentUserId));
        }
        return commentDtos;
    }

    private async Task<ForumPostDto> MapToForumPostDto(ForumPost post, int? currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(post.UserId);
        var displayName = !string.IsNullOrEmpty(user?.FirstName) 
            ? $"{user.FirstName} {user.LastName}".Trim() 
            : user?.UserName;

        return new ForumPostDto
        {
            Id = post.Id,
            UserId = post.UserId,
            UserName = user?.UserName,
            UserDisplayName = displayName,
            UserDepartment = user?.Department,
            Title = post.Title,
            Content = post.Content,
            Category = post.Category,
            TargetDepartment = post.TargetDepartment,
            IsPinned = post.IsPinned,
            IsLocked = post.IsLocked,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsCurrentUser = currentUserId.HasValue && currentUserId.Value == post.UserId,
            CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0
        };
    }

    private async Task<ForumCommentDto> MapToForumCommentDto(ForumComment comment, int? currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(comment.UserId);
        var displayName = !string.IsNullOrEmpty(user?.FirstName) 
            ? $"{user.FirstName} {user.LastName}".Trim() 
            : user?.UserName;

        return new ForumCommentDto
        {
            Id = comment.Id,
            ForumPostId = comment.ForumPostId,
            UserId = comment.UserId,
            UserName = user?.UserName,
            UserDisplayName = displayName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsCurrentUser = currentUserId.HasValue && currentUserId.Value == comment.UserId
        };
    }
}