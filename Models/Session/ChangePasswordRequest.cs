using System.ComponentModel.DataAnnotations;

namespace FourSPM_WebService.Models.Session
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        public string? RecoveryCode { get; set; }

        public string? CurrentPassword { get; set; }
    }
}
