using System.ComponentModel.DataAnnotations;

namespace MealPlanner.Web.Models.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Вкажи ім’я.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажи email.")]
    [EmailAddress(ErrorMessage = "Некоректний формат email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажи пароль.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Пароль має містити мінімум 6 символів.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтверди пароль.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Паролі не співпадають.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}