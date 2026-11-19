using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class MemberCourse
{
    public int MemberCourseId { get; set; }

    public int MemberId { get; set; }

    public int CourseId { get; set; }

    public DateOnly StartDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual TrainingCourse Course { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual ICollection<SessionLog> SessionLogs { get; set; } = new List<SessionLog>();
}
