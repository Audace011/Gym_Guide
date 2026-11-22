using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Course
{
    [Authorize(Roles = "Member")]
    public class MyCourseModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRecommendationService _recommendationService;

        public MyCourseModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IRecommendationService recommendationService)
        {
            _context = context;
            _userManager = userManager;
            _recommendationService = recommendationService;
        }

        public MyCourseViewModel CourseData { get; set; } = new();
        public string? NoCourseMessage { get; set; }

        private async Task<GymGuide.Models.Member?> GetCurrentMemberAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Member;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound("Member record not found.");

            var activeCourse = await _recommendationService.GetActiveCourseForMemberAsync(member.MemberId);

            if (activeCourse == null)
            {
                try
                {
                    activeCourse = await _recommendationService.AssignRecommendedCourseAsync(member.MemberId);
                    activeCourse = await _recommendationService.GetActiveCourseForMemberAsync(member.MemberId);
                }
                catch
                {
                    // If no courses seeded yet
                }
            }

            if (activeCourse == null)
            {
                NoCourseMessage = "No active course is currently assigned. Please update your profile or contact an administrator.";
                return Page();
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            bool completedToday = activeCourse.SessionLogs.Any(sl => sl.Date == today && sl.Completed);

            var groupedExercises = activeCourse.Course.CourseExercises
                .GroupBy(ce => ce.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            CourseData = new MyCourseViewModel
            {
                MemberCourseId = activeCourse.MemberCourseId,
                CourseName = activeCourse.Course.Name,
                Description = activeCourse.Course.Description,
                DurationWeeks = activeCourse.Course.DurationWeeks,
                Level = activeCourse.Course.Level,
                Goal = activeCourse.Course.Goal,
                BmiCategory = activeCourse.Course.Bmicategory,
                StartDate = activeCourse.StartDate,
                Status = activeCourse.Status,
                CompletedSessionsCount = activeCourse.SessionLogs.Count(sl => sl.Completed),
                CompletedToday = completedToday,
                ExercisesByDay = groupedExercises
            };

            return Page();
        }

        public async Task<IActionResult> OnPostCompleteSessionAsync(int memberCourseId)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            var memberCourse = await _context.MemberCourses
                .Include(mc => mc.SessionLogs)
                .FirstOrDefaultAsync(mc => mc.MemberCourseId == memberCourseId && mc.MemberId == member.MemberId);

            if (memberCourse == null)
                return NotFound();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var existingToday = memberCourse.SessionLogs.FirstOrDefault(sl => sl.Date == today);

            if (existingToday == null)
            {
                var sessionLog = new SessionLog
                {
                    MemberCourseId = memberCourseId,
                    Date = today,
                    Completed = true
                };
                _context.SessionLogs.Add(sessionLog);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Great job! Today's workout session has been marked as completed.";
            }
            else
            {
                existingToday.Completed = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Session completion updated for today.";
            }

            return RedirectToPage("/Course/MyCourse");
        }
    }
}
