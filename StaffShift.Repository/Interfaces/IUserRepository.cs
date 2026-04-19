using StaffShift.Core.Entities;

namespace StaffShift.Repository.Interfaces;

/// <summary>
/// Repository interface for User-specific operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserWithSubordinatesAsync(int userId);
    Task<IEnumerable<User>> GetUsersByDepartmentAsync(string department);
    Task<IEnumerable<User>> GetWorkersByManagerAsync(int managerId);
    Task<IEnumerable<User>> GetAllWorkersAsync();
    Task<IEnumerable<User>> GetAllManagersAsync();
    Task<IEnumerable<User>> GetUsersWithoutManagerAsync();
}