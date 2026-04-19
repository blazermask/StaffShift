using StaffShift.Core.Entities;

namespace StaffShift.Repository.Interfaces;

/// <summary>
/// Repository interface for Forum-specific operations
/// </summary>
public interface IForumRepository : IRepository<ForumPost>
{
    Task<IEnumerable<ForumPost>> GetRecentPostsAsync(int count = 20);
    Task<IEnumerable<ForumPost>> GetPostsByDepartmentAsync(string? department);
    Task<ForumPost?> GetPostWithCommentsAsync(int postId);
    Task<IEnumerable<ForumPost>> GetPostsByUserAsync(int userId);
    Task<IEnumerable<ForumPost>> GetPinnedPostsAsync();
}

public interface IForumCommentRepository : IRepository<ForumComment>
{
    Task<IEnumerable<ForumComment>> GetCommentsByPostAsync(int postId);
}