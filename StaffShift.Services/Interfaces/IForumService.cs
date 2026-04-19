using StaffShift.Core.DTOs;

namespace StaffShift.Services.Interfaces;

/// <summary>
/// Service interface for forum operations
/// </summary>
public interface IForumService
{
    Task<ForumPostDto?> GetPostByIdAsync(int postId, int? currentUserId = null);
    Task<IEnumerable<ForumPostDto>> GetRecentPostsAsync(int count = 20, int? currentUserId = null);
    Task<IEnumerable<ForumPostDto>> GetPostsByUserAsync(int userId, int? currentUserId = null);
    Task<IEnumerable<ForumPostDto>> GetPostsByDepartmentAsync(string? department, int? currentUserId = null);
    Task<(bool Success, string Message, ForumPostDto? Post)> CreatePostAsync(CreateForumPostDto model, int userId);
    Task<(bool Success, string Message, ForumPostDto? Post)> UpdatePostAsync(UpdateForumPostDto model, int userId);
    Task<(bool Success, string Message)> DeletePostAsync(int postId, int userId, bool isAdmin);
    Task<(bool Success, string Message, ForumPostDto? Post)> TogglePinAsync(int postId, int userId, bool isManager);
    Task<(bool Success, string Message, ForumPostDto? Post)> ToggleLockAsync(int postId, int userId, bool isManager);
    Task<(bool Success, string Message, ForumCommentDto? Comment)> AddCommentAsync(CreateForumCommentDto model, int userId);
    Task<(bool Success, string Message)> DeleteCommentAsync(int commentId, int userId, bool isAdmin);
    Task<IEnumerable<ForumCommentDto>> GetCommentsByPostAsync(int postId, int? currentUserId = null);
}