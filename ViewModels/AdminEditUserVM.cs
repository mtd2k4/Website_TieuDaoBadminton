using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModels
{
    public class AdminEditUserVM
    {
        [Required]
        public string Id { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        public string Role { get; set; } = "Customer";

        public bool IsLocked { get; set; }
    }
}
