using System;
using System.Linq;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Services
{
    public interface IRecommendationService
    {
        Task<TrainingCourse?> GetRecommendedCourseAsync(int memberId);
        Task<MemberCourse> AssignRecommendedCourseAsync(int memberId);
        Task<MemberCourse?> GetActiveCourseForMemberAsync(int memberId);
        Task<MemberCourse?> GetActiveCourseAsync(int memberId);
        Task<MemberCourse?> RecommendCourseAsync(int memberId);
    }

    // FR4: recommends a Beginner course matching the member's goal and latest BMI category.
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBmiService _bmiService;

        public RecommendationService(ApplicationDbContext context, IBmiService bmiService)
        {
            _context = context;
            _bmiService = bmiService;
        }

        // Returns the member's currently active course, or null if they have none.
        public Task<MemberCourse?> GetActiveCourseAsync(int memberId)
        {
            return _context.MemberCourses
                .Include(mc => mc.Course)
                .FirstOrDefaultAsync(mc => mc.MemberId == memberId && mc.Status == "Active");
        }

        // Stores a recommended course as the member's Active course.
        public async Task<MemberCourse?> RecommendCourseAsync(int memberId)
        {
            var existing = await GetActiveCourseAsync(memberId);
            if (existing != null)
            {
                return existing;
            }

            var course = await GetRecommendedCourseAsync(memberId);
            if (course == null)
            {
                return null;
            }

            var memberCourse = new MemberCourse
            {
                MemberId = memberId,
                CourseId = course.CourseId,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Active",
                Course = course
            };

            _context.MemberCourses.Add(memberCourse);
            await _context.SaveChangesAsync();
            return memberCourse;
        }

        public async Task<TrainingCourse?> GetRecommendedCourseAsync(int memberId)
        {
            var member = await _context.Members
                .Include(m => m.WeightLogs)
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return null;

            string bmiCategory = "Normal";
            var latestLog = member.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
            if (latestLog != null)
            {
                bmiCategory = latestLog.Bmicategory;
            }

            string goal = member.Goal ?? "Stay fit";

            // Find matching beginner course by BMI Category & Goal
            var course = await _context.TrainingCourses
                .Where(c => c.Level == "Beginner" && c.Bmicategory == bmiCategory && c.Goal == goal)
                .FirstOrDefaultAsync();

            // Fallback 1: match Goal and Beginner level
            if (course == null)
            {
                course = await _context.TrainingCourses
                    .Where(c => c.Level == "Beginner" && c.Goal == goal)
                    .FirstOrDefaultAsync();
            }

            // Fallback 2: any beginner course
            if (course == null)
            {
                course = await _context.TrainingCourses
                    .Where(c => c.Level == "Beginner")
                    .FirstOrDefaultAsync();
            }

            return course;
        }

        public async Task<MemberCourse> AssignRecommendedCourseAsync(int memberId)
        {
            var existingActive = await GetActiveCourseForMemberAsync(memberId);
            if (existingActive != null)
            {
                return existingActive;
            }

            var course = await GetRecommendedCourseAsync(memberId);
            if (course == null)
            {
                throw new InvalidOperationException("No suitable training course found to recommend.");
            }

            var memberCourse = new MemberCourse
            {
                MemberId = memberId,
                CourseId = course.CourseId,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Active"
            };

            _context.MemberCourses.Add(memberCourse);
            await _context.SaveChangesAsync();

            return memberCourse;
        }

        public async Task<MemberCourse?> GetActiveCourseForMemberAsync(int memberId)
        {
            return await _context.MemberCourses
                .Include(mc => mc.Course)
                    .ThenInclude(c => c.CourseExercises)
                        .ThenInclude(ce => ce.Exercise)
                .Include(mc => mc.SessionLogs)
                .Where(mc => mc.MemberId == memberId && mc.Status == "Active")
                .OrderByDescending(mc => mc.MemberCourseId)
                .FirstOrDefaultAsync();
        }
    }
}
