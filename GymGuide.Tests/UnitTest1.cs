
using GymGuide.Services;

namespace GymGuide.Tests;

public class BmiServiceTests
{
    private readonly BmiService _bmiService = new();

    [Theory]
    [InlineData(18.4, "Underweight")]
    [InlineData(18.5, "Normal")]
    [InlineData(24.9, "Normal")]
    [InlineData(25.0, "Overweight")]
    [InlineData(29.9, "Overweight")]
    [InlineData(30.0, "Obese")]
    public void CalculateBmi_ReturnsCorrectCategory(
        decimal expectedBmi, string expectedCategory)
    {
        // Use a height of 100 cm so weight equals BMI
        var result = _bmiService.CalculateBmi(expectedBmi, 100m);

        Assert.Equal(expectedBmi, result.Bmi);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Fact]
    public void CalculateBmi_UsesHeightInMetresAndRoundsToOneDecimalPlace()
    {
        var result = _bmiService.CalculateBmi(70m, 175m);

        Assert.Equal(22.9m, result.Bmi);
        Assert.Equal("Normal", result.Category);
    }

    [Theory]
    [InlineData(0, 170)]
    [InlineData(70, 0)]
    public void CalculateBmi_RejectsNonPositiveMeasurements(
        decimal weightKg, decimal heightCm)
    {
        Assert.Throws<ArgumentException>(
            () => _bmiService.CalculateBmi(weightKg, heightCm));
    }
}
