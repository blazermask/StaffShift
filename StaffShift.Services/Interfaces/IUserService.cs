using StaffShift.Core.DTOs;

namespace StaffShift.Services.Interfaces;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    Task<(bool Success, string Message, UserDto? User)> RegisterAsync(RegisterDto model, string? ipAddress = null);
    Task<(bool Success, string Message, UserDto? User)> LoginAsync(LoginDto model, string? ipAddress = null);
    Task<UserDto?> GetUserByIdAsync(int userId, int? currentUserId = null);
    Task<UserDto?> GetUserByUsernameAsync(string username, int? currentUserId = null);
    Task<(bool Success, string Message, UserDto? User)> UpdateProfileAsync(int userId, UpdateProfileDto model);
    Task<(bool Success, string Message)> AssignManagerAsync(int workerId, int managerId);
    Task<(IEnumerable<UserDto> Users, int TotalCount)> SearchUsersAsync(string searchTerm, int pageIndex, int pageSize, int? currentUserId = null);
    Task<IEnumerable<UserDto>> GetSubordinatesAsync(int managerId, int? currentUserId = null);
    Task<IEnumerable<UserDto>> GetAllManagersAsync(int? currentUserId = null);
    Task<IEnumerable<UserDto>> GetUsersWithoutManagerAsync(int? currentUserId = null);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(int? currentUserId = null);
    Task<IEnumerable<UserDto>> GetAllWorkersAsync(int? currentUserId = null);
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto model);
    Task<bool> IsInRoleAsync(int userId, string role);
}