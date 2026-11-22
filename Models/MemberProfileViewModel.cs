
using System.ComponentModel.DataAnnotations;

namespace GymGuide.ViewModels;

public class MemberProfileViewModel
{
    [Required]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    public string? Gender { get; set; }

    [Required]
    [Range(50, 250)]
    public decimal? HeightCm { get; set; }

    [Required]
    [Range(1, 500)]
    public decimal? WeightKg { get; set; }

    [Required]
    [RegularExpression(
        "^(Lose weight|Build muscle|Stay fit)$",
        ErrorMessage = "Select a listed fitness goal.")]
    public string? Goal { get; set; }

    public decimal? Bmi { get; set; }

    public string? BmiCategory { get; set; }

    public string? RecommendedCourse { get; set; }
}
