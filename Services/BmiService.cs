
namespace GymGuide.Services;

public class BmiService
{
    public (decimal Bmi, string Category) CalculateBmi(
        decimal weightKg,
        decimal heightCm)
    {
        if (weightKg <= 0)
            throw new ArgumentException("Weight must be greater than zero.");

        if (heightCm <= 0)
            throw new ArgumentException("Height must be greater than zero.");

        decimal heightMetres = heightCm / 100m;
        decimal bmi = weightKg / (heightMetres * heightMetres);

        decimal roundedBmi = Math.Round(
            bmi, 1, MidpointRounding.AwayFromZero);

        string category;

        if (bmi < 18.5m)
            category = "Underweight";
        else if (bmi < 25.0m)
            category = "Normal";
        else if (bmi < 30.0m)
            category = "Overweight";
        else
            category = "Obese";

        return (roundedBmi, category);
    }
}
