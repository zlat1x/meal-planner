namespace MealPlanner.Api.Models;

public class ApiLoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ApiRegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ApiAuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}
