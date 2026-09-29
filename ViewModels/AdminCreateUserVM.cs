using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModels
{
    public class AdminCreateUserVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(6)]
        public string Password { get; set; } = "User@123";

        public string? Role { get; set; } = "Customer";
    }
}
