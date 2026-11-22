using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Controllers
{
    [Authorize(Roles = "Member")]
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRecommendationService _recommendationService;

        public CourseController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IRecommendationService recommendationService)
        {
            _context = context;
            _userManager = userManager;
            _recommendationService = recommendationService;
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Member;
        }

        [HttpGet]
        public async Task<IActionResult> MyCourse()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound("Member record not found.");

            var activeCourse = await _recommendationService.GetActiveCourseForMemberAsync(member.MemberId);

            // Auto-assign if none active yet
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
                ViewBag.NoCourseMessage = "No active course is currently assigned. Please update your profile or contact an administrator.";
                return View(new MyCourseViewModel());
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            bool completedToday = activeCourse.SessionLogs.Any(sl => sl.Date == today && sl.Completed);

            // Group exercises by DayOfWeek
            var groupedExercises = activeCourse.Course.CourseExercises
                .GroupBy(ce => ce.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            var vm = new MyCourseViewModel
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

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSession(int memberCourseId)
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

            return RedirectToAction(nameof(MyCourse));
        }
    }
}
