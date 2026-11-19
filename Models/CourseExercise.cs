using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class CourseExercise
{
    public int CourseExerciseId { get; set; }

    public int CourseId { get; set; }

    public int ExerciseId { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public int? Sets { get; set; }

    public int? Reps { get; set; }

    public int? DurationMinutes { get; set; }

    public virtual TrainingCourse Course { get; set; } = null!;

    public virtual Exercise Exercise { get; set; } = null!;
}
