namespace CoffeeAnalytics.Domain.Entities;

/// <summary>
/// Platform user with role-based access for subscription model.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Free;
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public DateTime? DeletedAt { get; set; } // soft delete

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

public enum UserRole
{
    Free,
    Premium,
    Admin
}
