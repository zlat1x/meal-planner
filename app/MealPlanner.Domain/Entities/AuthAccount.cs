namespace MealPlanner.Domain.Entities;

public class AuthAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}