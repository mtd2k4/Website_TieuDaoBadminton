using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModel.Auth
{
    public class RegisterVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";

        public string? ReturnUrl { get; set; }
    }
}
