using Microsoft.EntityFrameworkCore;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;

namespace StaffShift.Repository.Repositories;

/// <summary>
/// Repository implementation for Forum-specific operations
/// </summary>
public class ForumRepository : Repository<ForumPost>, IForumRepository
{
    public ForumRepository(StaffShiftDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ForumPost>> GetRecentPostsAsync(int count = 20)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<ForumPost>> GetPostsByDepartmentAsync(string? department)
    {
        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(p => p.TargetDepartment == null || p.TargetDepartment == department);
        }

        return await query
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<ForumPost?> GetPostWithCommentsAsync(int postId)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
    }

    public async Task<IEnumerable<ForumPost>> GetPostsByUserAsync(int userId)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.Comments)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ForumPost>> GetPinnedPostsAsync()
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.IsPinned && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}

public class ForumCommentRepository : Repository<ForumComment>, IForumCommentRepository
{
    public ForumCommentRepository(StaffShiftDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ForumComment>> GetCommentsByPostAsync(int postId)
    {
        return await _dbSet
            .Include(c => c.User)
            .Where(c => c.ForumPostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }
}