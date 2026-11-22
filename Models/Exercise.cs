using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class Exercise
{
    public int ExerciseId { get; set; }

    public string Name { get; set; } = null!;

    public string? MuscleGroup { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<CourseExercise> CourseExercises { get; set; } = new List<CourseExercise>();
}
