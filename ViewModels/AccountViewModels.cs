using System.ComponentModel.DataAnnotations;

namespace GymGuide.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Height in cm is required for BMI calculation.")]
        [Range(50, 260, ErrorMessage = "Please enter a valid height between 50 and 260 cm.")]
        [Display(Name = "Height (cm)")]
        public decimal HeightCm { get; set; }

        [Required(ErrorMessage = "Current Weight in kg is required.")]
        [Range(20, 350, ErrorMessage = "Please enter a valid weight between 20 and 350 kg.")]
        [Display(Name = "Current Weight (kg)")]
        public decimal InitialWeightKg { get; set; }

        [Required(ErrorMessage = "Please select a fitness goal.")]
        [Display(Name = "Fitness Goal")]
        public string Goal { get; set; } = "Stay fit";
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
