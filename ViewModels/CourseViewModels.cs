using GymGuide.Models;

namespace GymGuide.ViewModels
{
    public class MyCourseViewModel
    {
        public int MemberCourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationWeeks { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public string BmiCategory { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public string Status { get; set; } = "Active";

        public int CompletedSessionsCount { get; set; }
        public bool CompletedToday { get; set; }

        public Dictionary<string, List<CourseExercise>> ExercisesByDay { get; set; } 
            = new Dictionary<string, List<CourseExercise>>();
    }
}
