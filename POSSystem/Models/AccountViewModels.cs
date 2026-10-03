using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models
{
    // ViewModel for Login form
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    // ViewModel for Register form (includes Role assignment)
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select an account role")]
        public string Role { get; set; } = "Cashier";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
