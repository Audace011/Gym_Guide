using GymGuide.Data;
using GymGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Services;

// FR5: exercises for a course, and logging of completed sessions.
public class WorkoutService
{
    private readonly ApplicationDbContext _db;

    public WorkoutService(ApplicationDbContext db)
    {
        _db = db;
    }

    // All exercises planned for a course, with the Exercise details loaded.
    public Task<List<CourseExercise>> GetExercisesAsync(int courseId)
    {
        return _db.CourseExercises
            .Include(ce => ce.Exercise)
            .Where(ce => ce.CourseId == courseId)
            .OrderBy(ce => ce.CourseExerciseId)
            .ToListAsync();
    }

    // Number of sessions the member has completed on this course.
    public Task<int> GetCompletedSessionCountAsync(int memberCourseId)
    {
        return _db.SessionLogs.CountAsync(s => s.MemberCourseId == memberCourseId && s.Completed);
    }

    // True if a session has already been logged today for this course.
    public Task<bool> HasLoggedTodayAsync(int memberCourseId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _db.SessionLogs.AnyAsync(s => s.MemberCourseId == memberCourseId && s.Date == today);
    }

    // Logs today's session as completed.
    // Returns false if the course is not this member's active course, or today is already logged.
    public async Task<bool> LogTodayAsync(int memberId, int memberCourseId)
    {
        var ownsActiveCourse = await _db.MemberCourses.AnyAsync(mc =>
            mc.MemberCourseId == memberCourseId &&
            mc.MemberId == memberId &&
            mc.Status == "Active");
        if (!ownsActiveCourse)
        {
            return false;
        }

        if (await HasLoggedTodayAsync(memberCourseId))
        {
            return false;
        }

        _db.SessionLogs.Add(new SessionLog
        {
            MemberCourseId = memberCourseId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Completed = true
        });
        await _db.SaveChangesAsync();
        return true;
    }
}