using System.ComponentModel.DataAnnotations;

namespace GymGuide.ViewModels;

public class MemberProgressViewModel
{
    [Required]
    [Range(1, 500)]
    public decimal? WeightKg { get; set; }

    public decimal? HeightCm { get; set; }

    public IReadOnlyList<WeightHistoryEntryViewModel> Measurements { get; set; } =
        Array.Empty<WeightHistoryEntryViewModel>();
}

public class WeightHistoryEntryViewModel
{
    public decimal WeightKg { get; set; }

    public decimal Bmi { get; set; }

    public string BmiCategory { get; set; } = string.Empty;

    public DateTime LoggedAt { get; set; }
}
