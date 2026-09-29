using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModel.Auth
{
    public class LoginVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }
}
