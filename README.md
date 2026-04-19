# StaffShift - Shift & Staff Management System

StaffShift is a comprehensive web-based application designed to help organizations manage employee shifts, time-off requests, and internal communication. Built with ASP.NET Core 8.0 MVC, it provides a professional interface for employees, managers, and executives to coordinate workforce management efficiently.

## Features

### For All Users
- **Dashboard**: Personalized dashboard with key metrics and quick actions
- **Shift Management**: View schedules, request shifts, clock in/out functionality
- **Time Off Requests**: Submit vacation, sick leave, and personal time requests
- **Forum**: Exchange work information with colleagues through posts and comments
- **Profile Management**: Update personal information and change passwords

### For Managers
- **Team Overview**: View and manage team members
- **Time Off Approvals**: Review and approve/reject team time-off requests
- **Shift Oversight**: Monitor team shifts and attendance

### For CEOs
- **Company Dashboard**: Organization-wide statistics and metrics
- **Employee Management**: Add, edit, and manage all employees
- **Department Overview**: View department structures and assignments
- **Reports**: Access company-wide analytics and reports
- **Manager Assignment**: Assign managers to workers

## User Roles

The system implements three distinct roles:

1. **CEO** - Full administrative access to all features
2. **Manager** - Team management capabilities, time-off approvals
3. **Worker** - Personal shift and time-off management

## Technology Stack

- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Architecture**: Layered architecture (Core, Data, Repository, Services, Web)
- **Frontend**: Bootstrap 5 with custom professional styling

## Project Structure

```
StaffShift/
├── StaffShift.Core/           # Entities and DTOs
│   ├── Entities/              # Domain models
│   └── DTOs/                  # Data transfer objects
├── StaffShift.Data/           # Database context and data initialization
├── StaffShift.Repository/     # Data access layer
│   ├── Interfaces/            # Repository interfaces
│   └── Repositories/          # Repository implementations
├── StaffShift.Services/       # Business logic layer
│   ├── Interfaces/            # Service interfaces
│   └── Services/              # Service implementations
├── StaffShift.Web/            # Web application
│   ├── Controllers/           # MVC controllers
│   ├── Views/                 # Razor views
│   └── wwwroot/               # Static files
└── StaffShift.Tests/          # Unit tests
```

## Installation

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/StaffShift.git
   cd StaffShift
   ```

2. **Configure the database connection**
   
   Update the connection string in `StaffShift.Web/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=your-server;Database=StaffShiftDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

3. **Apply database migrations**
   ```bash
   cd StaffShift.Web
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   
   Navigate to `https://localhost:5001` or `http://localhost:5000`

## Default Accounts

The system creates default accounts during initialization:

| Role    | Email      | Password     |
|---------|------------|--------------|
| CEO     | ceo@company.com    | CEO123!      |
| Manager | manager@company.com | Manager123!  |
| Worker  | worker@company.com | Worker123!   |

**Important**: Change these passwords immediately after first login in a production environment.

## Security Features

- **IP-based Rate Limiting**: Prevents brute-force attacks on login/registration
- **Password Hashing**: Secure password storage using ASP.NET Core Identity
- **Role-based Authorization**: Access control based on user roles
- **CSRF Protection**: Anti-forgery tokens on all forms
- **Input Validation**: Server-side validation on all user inputs

## Configuration Options

### Registration Limits
- Maximum registration attempts per IP: 2 per day
- Login lockout: 3 failed attempts, 5-minute lockout

### Time Off Defaults
- Vacation days: 20 per year
- Sick days: 10 per year
- Personal days: 5 per year

These can be configured in `DataInitializer.cs`.

## API Reference

### Controllers

| Controller | Purpose |
|------------|---------|
| AccountController | Authentication, registration, profile management |
| DashboardController | Main landing page with role-based content |
| ShiftController | Shift scheduling and clock in/out |
| TimeOffController | Time-off request management |
| ForumController | Forum posts and comments |
| ManagerController | Team management for managers |
| CEOController | Company-wide administration |

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and feature requests, please use the GitHub Issues page.

---

**StaffShift** - Streamlining workforce management for modern organizations.