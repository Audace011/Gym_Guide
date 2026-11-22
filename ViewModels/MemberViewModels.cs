using System.ComponentModel.DataAnnotations;
using GymGuide.Models;

namespace GymGuide.ViewModels
{
    public class ProfileViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Required(ErrorMessage = "Height in cm is required.")]
        [Range(50, 260, ErrorMessage = "Enter height between 50 and 260 cm.")]
        [Display(Name = "Height (cm)")]
        public decimal? HeightCm { get; set; }

        [Required(ErrorMessage = "Please select a fitness goal.")]
        [Display(Name = "Fitness Goal")]
        public string? Goal { get; set; }

        public decimal LatestWeightKg { get; set; }
        public decimal LatestBmi { get; set; }
        public string LatestBmiCategory { get; set; } = "Normal";

        public string? AssignedTrainerName { get; set; }
        public string? AssignedTrainerSpecialization { get; set; }
        public string? LatestTrainerRequestStatus { get; set; }
    }

    public class WeightTrackingViewModel
    {
        [Required(ErrorMessage = "Please enter your weight.")]
        [Range(20, 350, ErrorMessage = "Enter valid weight between 20 and 350 kg.")]
        [Display(Name = "New Weight (kg)")]
        public decimal NewWeightKg { get; set; }

        public decimal CurrentBmi { get; set; }
        public string CurrentBmiCategory { get; set; } = string.Empty;

        public List<WeightLog> Logs { get; set; } = new List<WeightLog>();

        // Chart.js data
        public string ChartDatesJson { get; set; } = "[]";
        public string ChartWeightsJson { get; set; } = "[]";
    }
}
