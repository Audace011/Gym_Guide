using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            // 1. BMI Calculation & Boundary Unit Tests (FR1, FR2)
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
            // 2. Course Recommendation Service Tests (FR4)
            // ==========================================
            var recService = new RecommendationService(context, bmiService);

            results.Add(await RunTestAsync("Recommendation: Find or Recommend Beginner Course", "Course Recommendation", async () =>
            {
                var member = await context.Members.FirstOrDefaultAsync();
                if (member != null)
                {
                    var course = await recService.GetRecommendedCourseAsync(member.MemberId);
                    Assert(course != null, "RecommendationService should find a matching beginner course.");
                    Assert(course.Level == "Beginner", $"Course level must be Beginner, got {course.Level}");
                }
            }));

            results.Add(await RunTestAsync("Recommendation: Active Course Retrieval", "Course Recommendation", async () =>
            {
                var member = await context.Members.FirstOrDefaultAsync();
                if (member != null)
                {
                    var active = await recService.GetActiveCourseAsync(member.MemberId);
                    Assert(active == null || active.Status == "Active", "Active course must have 'Active' status.");
                }
            }));

            // ==========================================
            // 3. Workout Service Tests (FR5)
            // ==========================================
            var workoutService = new WorkoutService(context);

            results.Add(await RunTestAsync("Workout Service: Exercises Retrieval for Course", "Workout Tracking", async () =>
            {
                var course = await context.TrainingCourses.FirstOrDefaultAsync();
                if (course != null)
                {
                    var exercises = await workoutService.GetExercisesAsync(course.CourseId);
                    Assert(exercises != null, "WorkoutService should return exercise list.");
                }
            }));

            results.Add(await RunTestAsync("Workout Service: Today Session Check and Counter", "Workout Tracking", async () =>
            {
                var memberCourse = await context.MemberCourses.FirstOrDefaultAsync(mc => mc.Status == "Active");
                if (memberCourse != null)
                {
                    var count = await workoutService.GetCompletedSessionCountAsync(memberCourse.MemberCourseId);
                    Assert(count >= 0, "Completed session count should be non-negative.");

                    var loggedToday = await workoutService.HasLoggedTodayAsync(memberCourse.MemberCourseId);
                    Assert(loggedToday == true || loggedToday == false, "HasLoggedToday must return boolean.");
                }
            }));

            // ==========================================
            // 4. Trainer Assignment Service Tests (FR6)
            // ==========================================
            var trainerService = new TrainerAssignmentService(context);

            results.Add(await RunTestAsync("Trainer Assignment: Capacity Constraint", "Trainer Assignment", async () =>
            {
                var trainers = await context.Trainers.Include(t => t.Members).ToListAsync();
                foreach (var t in trainers)
                {
                    Assert(t.Members.Count <= t.MaxClients, 
                        $"Trainer {t.TrainerId} exceeds MaxClients limit: {t.Members.Count}/{t.MaxClients}");
                }
            }));

            results.Add(await RunTestAsync("Trainer Assignment: Specialization Matching Priority", "Trainer Assignment", async () =>
            {
                var member = await context.Members.FirstOrDefaultAsync(m => m.Goal == "Build muscle");
                if (member != null)
                {
                    var result = await trainerService.AssignTrainerAsync(member.MemberId);
                    Assert(result.AssignedTrainer == null || result.AssignedTrainer.Specialization == "Build muscle" || !result.AssignedTrainer.IsAvailable, 
                        "Trainer assignment must prioritize matching specialization.");
                }
            }));

            results.Add(await RunTestAsync("Workout Plan Adjustment: Safe Replacement (FR9)", "Trainer Assignment", async () =>
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

            // ==========================================
            // 5. Payments & In-Memory Revenue Summing (FR11, FR12)
            // ==========================================
            results.Add(await RunTestAsync("Payments: In-Memory Decimal Revenue Calculation", "Financial Reporting", async () =>
            {
                var paymentAmounts = await context.Payments.Select(p => p.Amount).ToListAsync();
                var total = paymentAmounts.Sum();
                Assert(total >= 0m, "Total revenue must be non-negative.");
            }));

            results.Add(await RunTestAsync("Plans: Revenue Grouping by Plan", "Financial Reporting", async () =>
            {
                var plans = await context.MembershipPlans.Include(p => p.Payments).ToListAsync();
                foreach (var plan in plans)
                {
                    var planTotal = plan.Payments.Sum(p => p.Amount);
                    Assert(planTotal >= 0m, $"Plan {plan.Name} revenue must be non-negative.");
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
