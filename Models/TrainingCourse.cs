using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class TrainingCourse
{
    public int CourseId { get; set; }

    public string Name { get; set; } = null!;

    public string Bmicategory { get; set; } = null!;

    public string Goal { get; set; } = null!;

    public string Level { get; set; } = null!;

    public int DurationWeeks { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<CourseExercise> CourseExercises { get; set; } = new List<CourseExercise>();

    public virtual ICollection<MemberCourse> MemberCourses { get; set; } = new List<MemberCourse>();
}
