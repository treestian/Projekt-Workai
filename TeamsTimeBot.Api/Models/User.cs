namespace TeamsTimeBot.Api.Models;

public enum UserRole
{
    Employee,
    Admin
}

public class User
{
    public int Id { get; set; }

    public string AzureId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public string? UserPrincipalName { get; set; }

    public bool IsActive { get; set; } = true;

    public UserRole Role { get; set; } = UserRole.Employee;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

