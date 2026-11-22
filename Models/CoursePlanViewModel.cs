namespace GymGuide.Models;

// Data for the My Course page.
public class CoursePlanViewModel
{
    public MemberCourse? ActiveCourse { get; set; }

    // Exercises grouped by day, e.g. "Monday" -> list of exercises.
    public Dictionary<string, List<CourseExercise>> ExercisesByDay { get; set; } = new();

    public int CompletedSessions { get; set; }

    public bool LoggedToday { get; set; }

    // Message shown when there is no active course.
    public string? Message { get; set; }
}