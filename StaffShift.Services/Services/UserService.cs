using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StaffShift.Core.DTOs;
using StaffShift.Core.Entities;
using StaffShift.Data;
using StaffShift.Repository.Interfaces;
using StaffShift.Services.Interfaces;

namespace StaffShift.Services.Services;

/// <summary>
/// Service implementation for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly StaffShiftDbContext _context;

    public UserService(IUserRepository userRepository, UserManager<User> userManager, StaffShiftDbContext context)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _context = context;
    }

    public async Task<(bool Success, string Message, UserDto? User)> RegisterAsync(RegisterDto model, string? ipAddress = null)
    {
        // Check IP-based registration limit (2 accounts per day)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var recentRegistrations = await _context.RegistrationRecords
                .CountAsync(r => r.IpAddress == ipAddress && r.CreatedAt >= oneDayAgo);

            if (recentRegistrations >= 2)
            {
                return (false, "Registration limit reached. You can only create 2 accounts per day.", null);
            }
        }

        // Check if username already exists
        var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
        if (existingUser != null)
        {
            return (false, "Username is already taken.", null);
        }

        // Check if email already exists
        var emailUser = await _userRepository.GetByEmailAsync(model.Email);
        if (emailUser != null)
        {
            return (false, "Email is already registered.", null);
        }

        // Validate manager exists if ManagerId is provided
        if (model.ManagerId.HasValue)
        {
            var manager = await _userRepository.GetByIdAsync(model.ManagerId.Value);
            if (manager == null)
            {
                return (false, "Specified manager does not exist.", null);
            }
        }

        // Create new user
        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmployeeId = model.EmployeeId,
            Department = model.Department,
            Position = model.Position,
            ManagerId = model.ManagerId,
            HireDate = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        // Add to Worker role by default
        await _userManager.AddToRoleAsync(user, "Worker");

        // Record the registration for IP-based rate limiting
        if (!string.IsNullOrEmpty(ipAddress))
        {
            _context.RegistrationRecords.Add(new RegistrationRecord
            {
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        var userDto = await MapToUserDto(user, user.Id);
        return (true, "Registration successful!", userDto);
    }

    public async Task<(bool Success, string Message, UserDto? User)> LoginAsync(LoginDto model, string? ipAddress = null)
    {
        ipAddress = ipAddress ?? "unknown";

        // Check IP-based lockout (3 failed attempts, 5 minute lockout)
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        var recentFailedAttempts = await _context.LoginAttempts
            .CountAsync(l => l.IpAddress == ipAddress && !l.WasSuccessful && l.AttemptedAt >= fiveMinutesAgo);

        if (recentFailedAttempts >= 3)
        {
            var firstFailedAttempt = await _context.LoginAttempts
                .Where(l => l.IpAddress == ipAddress && !l.WasSuccessful && l.AttemptedAt >= fiveMinutesAgo)
                .OrderBy(l => l.AttemptedAt)
                .FirstOrDefaultAsync();

            if (firstFailedAttempt != null)
            {
                var lockoutEnd = firstFailedAttempt.AttemptedAt.AddMinutes(5);
                var remainingMinutes = Math.Ceiling((lockoutEnd - DateTime.UtcNow).TotalMinutes);
                if (remainingMinutes > 0)
                {
                    return (false, $"Too many failed login attempts. Please try again in {remainingMinutes} minute(s).", null);
                }
            }
        }

        var user = await _userRepository.GetByUsernameAsync(model.UsernameOrEmail);
        if (user == null)
        {
            user = await _userRepository.GetByEmailAsync(model.UsernameOrEmail);
        }

        if (user == null)
        {
            await RecordLoginAttempt(ipAddress, model.UsernameOrEmail, false);
            return (false, "Invalid username or password.", null);
        }

        if (!user.IsActive)
        {
            return (false, "Your account has been deactivated. Please contact your manager.", null);
        }

        // Verify password
        var result = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!result)
        {
            await RecordLoginAttempt(ipAddress, model.UsernameOrEmail, false);
            return (false, "Invalid username or password.", null);
        }

        await RecordLoginAttempt(ipAddress, user.UserName, true);

        var userDto = await MapToUserDto(user, user.Id);
        return (true, "Login successful!", userDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId, int? currentUserId = null)
    {
        var user = await _userRepository.GetUserWithSubordinatesAsync(userId);
        if (user == null) return null;

        return await MapToUserDto(user, currentUserId);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username, int? currentUserId = null)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return null;

        return await MapToUserDto(user, currentUserId);
    }

    public async Task<(bool Success, string Message, UserDto? User)> UpdateProfileAsync(int userId, UpdateProfileDto model)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        user.FirstName = model.FirstName ?? user.FirstName;
        user.LastName = model.LastName ?? user.LastName;
        user.EmployeeId = model.EmployeeId ?? user.EmployeeId;
        user.Department = model.Department ?? user.Department;
        user.Position = model.Position ?? user.Position;
        user.ProfileImageUrl = model.ProfileImageUrl ?? user.ProfileImageUrl;
        user.ManagerId = model.ManagerId ?? user.ManagerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var userDto = await MapToUserDto(user, userId);
        return (true, "Profile updated successfully!", userDto);
    }

    public async Task<(bool Success, string Message)> AssignManagerAsync(int workerId, int managerId)
    {
        var worker = await _userRepository.GetByIdAsync(workerId);
        if (worker == null)
        {
            return (false, "Worker not found.");
        }

        var manager = await _userRepository.GetByIdAsync(managerId);
        if (manager == null)
        {
            return (false, "Manager not found.");
        }

        // Verify the assigned manager has the Manager role
        var isManager = await _userManager.IsInRoleAsync(manager, "Manager");
        if (!isManager)
        {
            return (false, "The specified user is not a manager.");
        }

        worker.ManagerId = managerId;
        worker.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(worker);
        await _userRepository.SaveChangesAsync();

        return (true, "Manager assigned successfully!");
    }

    public async Task<(IEnumerable<UserDto> Users, int TotalCount)> SearchUsersAsync(string searchTerm, int pageIndex, int pageSize, int? currentUserId = null)
    {
        var users = await _userRepository.GetAllAsync();
        var filteredUsers = users.Where(u => 
            (u.UserName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.LastName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.Department?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        var totalCount = filteredUsers.Count;
        var pagedUsers = filteredUsers
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        var userDtos = new List<UserDto>();
        foreach (var user in pagedUsers)
        {
            userDtos.Add(await MapToUserDto(user, currentUserId));
        }

        return (userDtos, totalCount);
    }

    public async Task<IEnumerable<UserDto>> GetSubordinatesAsync(int managerId, int? currentUserId = null)
    {
        var subordinates = await _userRepository.GetWorkersByManagerAsync(managerId);
        var userDtos = new List<UserDto>();
        foreach (var user in subordinates)
        {
            userDtos.Add(await MapToUserDto(user, currentUserId));
        }
        return userDtos;
    }

    public async Task<IEnumerable<UserDto>> GetAllManagersAsync(int? currentUserId = null)
    {
        var users = await _userRepository.GetAllAsync();
        var managerDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");
            if (isManager)
            {
                managerDtos.Add(await MapToUserDto(user, currentUserId));
            }
        }
        return managerDtos;
    }

    public async Task<IEnumerable<UserDto>> GetUsersWithoutManagerAsync(int? currentUserId = null)
    {
        var users = await _userRepository.GetUsersWithoutManagerAsync();
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            userDtos.Add(await MapToUserDto(user, currentUserId));
        }
        return userDtos;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(int? currentUserId = null)
    {
        var users = await _userRepository.GetAllAsync();
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            userDtos.Add(await MapToUserDto(user, currentUserId));
        }
        return userDtos;
    }

    public async Task<IEnumerable<UserDto>> GetAllWorkersAsync(int? currentUserId = null)
    {
        var users = await _userRepository.GetAllAsync();
        var workerDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var isWorker = await _userManager.IsInRoleAsync(user, "Worker");
            if (isWorker)
            {
                workerDtos.Add(await MapToUserDto(user, currentUserId));
            }
        }
        return workerDtos;
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto model)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found.");
        }

        var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordCheck)
        {
            return (false, "Current password is incorrect.");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return (true, "Password changed successfully!");
    }

    public async Task<bool> IsInRoleAsync(int userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        return await _userManager.IsInRoleAsync(user, role);
    }

    private async Task RecordLoginAttempt(string ipAddress, string? username, bool success)
    {
        _context.LoginAttempts.Add(new LoginAttempt
        {
            IpAddress = ipAddress,
            AttemptedUsername = username,
            WasSuccessful = success,
            AttemptedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task<UserDto> MapToUserDto(User user, int? currentUserId)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var managerName = user.ManagerId.HasValue
            ? (await _userRepository.GetByIdAsync(user.ManagerId.Value))?.UserName
            : null;

        var pendingRequests = await _context.TimeOffRequests
            .CountAsync(r => r.UserId == user.Id && r.Status == "Pending");

        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmployeeId = user.EmployeeId,
            Department = user.Department,
            Position = user.Position,
            ProfileImageUrl = user.ProfileImageUrl,
            HireDate = user.HireDate,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            ManagerId = user.ManagerId,
            ManagerName = managerName,
            SubordinateCount = user.Subordinates?.Count ?? 0,
            IsCurrentUser = currentUserId.HasValue && currentUserId.Value == user.Id,
            IsCEO = roles.Contains("CEO"),
            IsManager = roles.Contains("Manager"),
            IsWorker = roles.Contains("Worker"),
            PendingTimeOffRequests = pendingRequests,
            Roles = roles
        };
    }
}