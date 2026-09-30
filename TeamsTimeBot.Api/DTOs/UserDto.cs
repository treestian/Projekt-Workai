namespace TeamsTimeBot.Api.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string AzureId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? UserPrincipalName { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "Employee";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
