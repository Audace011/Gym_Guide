using GymGuide.Data;
using GymGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Services;

// FR4: recommends a Beginner course matching the member's goal and latest BMI category.
public class RecommendationService
{
    private readonly ApplicationDbContext _db;

    public RecommendationService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Returns the member's currently active course, or null if they have none.
    public Task<MemberCourse?> GetActiveCourseAsync(int memberId)
    {
        return _db.MemberCourses
            .Include(mc => mc.Course)
            .FirstOrDefaultAsync(mc => mc.MemberId == memberId && mc.Status == "Active");
    }

    // Stores a recommended course as the member's Active course.
    // Returns null if the member has no goal, no weight log, or no matching course.
    public async Task<MemberCourse?> RecommendCourseAsync(int memberId)
    {
        // Only one active course per member: if one already exists, keep it.
        var existing = await GetActiveCourseAsync(memberId);
        if (existing != null)
        {
            return existing;
        }

        var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == memberId);
        if (member == null || string.IsNullOrWhiteSpace(member.Goal))
        {
            return null;
        }

        var latestLog = await _db.WeightLogs
            .Where(w => w.MemberId == memberId)
            .OrderByDescending(w => w.LoggedAt)
            .FirstOrDefaultAsync();
        if (latestLog == null)
        {
            return null;
        }

        var course = await _db.TrainingCourses.FirstOrDefaultAsync(c =>
            c.Goal == member.Goal &&
            c.Bmicategory == latestLog.Bmicategory &&
            c.Level == "Beginner");
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

        _db.MemberCourses.Add(memberCourse);
        await _db.SaveChangesAsync();
        return memberCourse;
    }
}