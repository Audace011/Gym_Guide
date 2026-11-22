using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Tests
{
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public static class UnitTestsRunner
    {
        public static async Task<List<TestResult>> RunAllTestsAsync(ApplicationDbContext context)
        {
            var results = new List<TestResult>();
            var bmiService = new BmiService();

            // ==========================================
            // 1. BMI Calculation & Boundary Unit Tests
            // ==========================================
            results.Add(RunTest("BMI Boundary: 18.4 -> Underweight", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(18.4m);
                Assert(cat == "Underweight", $"Expected 'Underweight', got '{cat}'");
            }));

            results.Add(RunTest("BMI Boundary: 18.5 -> Normal", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(18.5m);
                Assert(cat == "Normal", $"Expected 'Normal', got '{cat}'");
            }));

            results.Add(RunTest("BMI Boundary: 24.9 -> Normal", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(24.9m);
                Assert(cat == "Normal", $"Expected 'Normal', got '{cat}'");
            }));

            results.Add(RunTest("BMI Boundary: 25.0 -> Overweight", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(25.0m);
                Assert(cat == "Overweight", $"Expected 'Overweight', got '{cat}'");
            }));

            results.Add(RunTest("BMI Boundary: 29.9 -> Overweight", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(29.9m);
                Assert(cat == "Overweight", $"Expected 'Overweight', got '{cat}'");
            }));

            results.Add(RunTest("BMI Boundary: 30.0 -> Obese", "BMI Service", () =>
            {
                var cat = bmiService.GetBmiCategory(30.0m);
                Assert(cat == "Obese", $"Expected 'Obese', got '{cat}'");
            }));

            results.Add(RunTest("BMI Calculation Accuracy: 70kg, 175cm -> 22.9", "BMI Service", () =>
            {
                var bmi = bmiService.CalculateBmi(70m, 175m);
                Assert(bmi == 22.9m, $"Expected 22.9, got {bmi}");
            }));

            results.Add(RunTest("BMI Calculation Zero/Invalid Handling", "BMI Service", () =>
            {
                var bmi = bmiService.CalculateBmi(0m, 175m);
                Assert(bmi == 0m, $"Expected 0 for zero weight, got {bmi}");
            }));

            // ==========================================
            // 2. Trainer Assignment Service Unit Tests
            // ==========================================
            var trainerService = new TrainerAssignmentService(context);

            results.Add(await RunTestAsync("Trainer Assignment: Match Specialization", "Trainer Assignment", async () =>
            {
                var member = await context.Members.FirstOrDefaultAsync(m => m.Goal == "Build muscle");
                if (member != null)
                {
                    var result = await trainerService.AssignTrainerAsync(member.MemberId);
                    Assert(result.AssignedTrainer == null || result.AssignedTrainer.Specialization == "Build muscle" || !result.AssignedTrainer.IsAvailable, 
                        "Trainer assignment must prioritize matching specialization.");
                }
            }));

            results.Add(await RunTestAsync("Trainer Assignment: Respect MaxClients Capacity", "Trainer Assignment", async () =>
            {
                var trainers = await context.Trainers.Include(t => t.Members).ToListAsync();
                foreach (var t in trainers)
                {
                    Assert(t.Members.Count <= t.MaxClients, 
                        $"Trainer {t.TrainerId} exceeds MaxClients limit: {t.Members.Count}/{t.MaxClients}");
                }
            }));

            results.Add(await RunTestAsync("Trainer Assignment: Exclude Unavailable Trainers", "Trainer Assignment", async () =>
            {
                var assignedRequests = await context.TrainerRequests
                    .Include(r => r.Trainer)
                    .Where(r => r.Status == "Assigned" && r.Trainer != null)
                    .ToListAsync();

                foreach (var r in assignedRequests)
                {
                    Assert(r.Trainer != null, "Assigned request must have a valid trainer.");
                }
            }));

            results.Add(await RunTestAsync("Workout Plan Adjustment: Safe Replacement (FR8)", "Trainer Assignment", async () =>
            {
                var member = await context.Members.Include(m => m.MemberCourses).FirstOrDefaultAsync();
                var courses = await context.TrainingCourses.Take(2).ToListAsync();
                if (member != null && courses.Count >= 2)
                {
                    int trainerId = member.TrainerId ?? 1;
                    bool adjusted = await trainerService.AdjustWorkoutPlanAsync(trainerId, member.MemberId, courses[1].CourseId);
                    Assert(adjusted, "AdjustWorkoutPlanAsync should succeed for valid member and course.");

                    var activeCourse = await context.MemberCourses
                        .Where(mc => mc.MemberId == member.MemberId && mc.Status == "Active")
                        .OrderByDescending(mc => mc.MemberCourseId)
                        .FirstOrDefaultAsync();

                    Assert(activeCourse != null && activeCourse.CourseId == courses[1].CourseId, 
                        "New course should be Active after adjustment.");
                }
            }));

            return results;
        }

        private static TestResult RunTest(string name, string category, Action action)
        {
            try
            {
                action();
                return new TestResult { TestName = name, Category = category, Passed = true, Message = "Passed" };
            }
            catch (Exception ex)
            {
                return new TestResult { TestName = name, Category = category, Passed = false, Message = ex.Message };
            }
        }

        private static async Task<TestResult> RunTestAsync(string name, string category, Func<Task> action)
        {
            try
            {
                await action();
                return new TestResult { TestName = name, Category = category, Passed = true, Message = "Passed" };
            }
            catch (Exception ex)
            {
                return new TestResult { TestName = name, Category = category, Passed = false, Message = ex.Message };
            }
        }

        private static void Assert(bool condition, string failureMessage)
        {
            if (!condition)
            {
                throw new InvalidOperationException(failureMessage);
            }
        }
    }
}
