using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static readonly string[] DayOrder =
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };

        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RecommendationService _recommendations;
        private readonly WorkoutService _workouts;

        public MyCourseModel(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RecommendationService recommendations,
            WorkoutService workouts)
        {
            _db = db;
            _userManager = userManager;
            _recommendations = recommendations;
            _workouts = workouts;
        }

        // Data shown on the page (supports Cindy's model and view model)
        public CoursePlanViewModel Plan { get; set; } = new();
        public MyCourseViewModel CourseData { get; set; } = new();
        public string? NoCourseMessage { get; set; }

        private async Task<GymGuide.Models.Member?> GetCurrentMemberAsync()
        {
            var identityId = _userManager.GetUserId(User);
            var user = await _db.Users
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityId);
            return user?.Member;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                Plan.Message = "No member profile was found for your account.";
                NoCourseMessage = Plan.Message;
                return Page();
            }

            var active = await _recommendations.GetActiveCourseAsync(member.MemberId)
                         ?? await _recommendations.RecommendCourseAsync(member.MemberId);

            if (active == null)
            {
                Plan.Message = "No course recommendation yet. Save your body details and weight first.";
                NoCourseMessage = Plan.Message;
                return Page();
            }

            Plan.ActiveCourse = active;
            Plan.CompletedSessions = await _workouts.GetCompletedSessionCountAsync(active.MemberCourseId);
            Plan.LoggedToday = await _workouts.HasLoggedTodayAsync(active.MemberCourseId);

            var exercises = await _workouts.GetExercisesAsync(active.CourseId);
            Plan.ExercisesByDay = exercises
                .GroupBy(e => e.DayOfWeek)
                .OrderBy(g => DayOrder.Contains(g.Key) ? Array.IndexOf(DayOrder, g.Key) : DayOrder.Length)
                .ToDictionary(g => g.Key, g => g.ToList());

            CourseData = new MyCourseViewModel
            {
                MemberCourseId = active.MemberCourseId,
                CourseName = active.Course.Name,
                Description = active.Course.Description,
                DurationWeeks = active.Course.DurationWeeks,
                Level = active.Course.Level,
                Goal = active.Course.Goal,
                BmiCategory = active.Course.Bmicategory,
                StartDate = active.StartDate,
                Status = active.Status,
                CompletedSessionsCount = Plan.CompletedSessions,
                CompletedToday = Plan.LoggedToday,
                ExercisesByDay = Plan.ExercisesByDay
            };

            return Page();
        }

        // POST: handler=MarkCompleted (Cindy's handler)
        public async Task<IActionResult> OnPostMarkCompletedAsync(int memberCourseId)
        {
            return await CompleteWorkoutSessionAsync(memberCourseId);
        }

        // POST: handler=CompleteSession
        public async Task<IActionResult> OnPostCompleteSessionAsync(int memberCourseId)
        {
            return await CompleteWorkoutSessionAsync(memberCourseId);
        }

        private async Task<IActionResult> CompleteWorkoutSessionAsync(int memberCourseId)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return Forbid();
            }

            var logged = await _workouts.LogTodayAsync(member.MemberId, memberCourseId);
            if (logged)
            {
                TempData["Message"] = "Session marked as completed. Well done!";
                TempData["SuccessMessage"] = "Great job! Today's workout session has been marked as completed.";
            }
            else
            {
                TempData["Message"] = "Could not log this session. It may already be logged today.";
                TempData["SuccessMessage"] = "Session completion already recorded for today.";
            }

            return RedirectToPage("/Course/MyCourse");
        }
    }
}
