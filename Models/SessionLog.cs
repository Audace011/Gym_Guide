using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class SessionLog
{
    public int SessionLogId { get; set; }

    public int MemberCourseId { get; set; }

    public DateOnly Date { get; set; }

    public bool Completed { get; set; }

    public virtual MemberCourse MemberCourse { get; set; } = null!;
}
