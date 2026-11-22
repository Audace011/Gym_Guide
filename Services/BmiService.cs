namespace GymGuide.Services
{
    public interface IBmiService
    {
        decimal CalculateBmi(decimal weightKg, decimal heightCm);
        string GetBmiCategory(decimal bmi);
    }

    public class BmiService : IBmiService
    {
        public decimal CalculateBmi(decimal weightKg, decimal heightCm)
        {
            if (heightCm <= 0 || weightKg <= 0)
                return 0;

            decimal heightInMeters = heightCm / 100m;
            decimal bmi = weightKg / (heightInMeters * heightInMeters);
            return Math.Round(bmi, 1);
        }

        public string GetBmiCategory(decimal bmi)
        {
            if (bmi < 18.5m)
                return "Underweight";
            if (bmi <= 24.9m)
                return "Normal";
            if (bmi <= 29.9m)
                return "Overweight";
            return "Obese";
        }
    }
}
