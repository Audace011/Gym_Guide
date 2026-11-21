using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Course;

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

    // Data shown on the page.
    public CoursePlanViewModel Plan { get; set; } = new();

    // Finds the Member record for the logged-in user.
    private async Task<Member?> GetCurrentMemberAsync()
    {
        var identityId = _userManager.GetUserId(User);
        var user = await _db.Users
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityId);
        return user?.Member;
    }

    // GET: /Course/MyCourse
    public async Task<IActionResult> OnGetAsync()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
        {
            Plan.Message = "No member profile was found for your account.";
            return Page();
        }

        // Always read the active course from the database, so trainer changes show up.
        var active = await _recommendations.GetActiveCourseAsync(member.MemberId)
                     ?? await _recommendations.RecommendCourseAsync(member.MemberId);

        if (active == null)
        {
            Plan.Message = "No course recommendation yet. Save your body details and weight first.";
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

        return Page();
    }

    // POST: /Course/MyCourse?handler=MarkCompleted
    public async Task<IActionResult> OnPostMarkCompletedAsync(int memberCourseId)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
        {
            return Forbid();
        }

        var logged = await _workouts.LogTodayAsync(member.MemberId, memberCourseId);
        TempData["Message"] = logged
            ? "Session marked as completed. Well done!"
            : "Could not log this session. It may already be logged today.";

        return RedirectToPage();
    }
}