using System.ComponentModel.DataAnnotations;

namespace MealPlanner.Web.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Вкажи email.")]
    [EmailAddress(ErrorMessage = "Некоректний формат email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажи пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}