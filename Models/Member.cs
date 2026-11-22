using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class Member
{
    public int MemberId { get; set; }

    public int UserId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public decimal? HeightCm { get; set; }

    public string? Goal { get; set; }

    public int? TrainerId { get; set; }

    public virtual ICollection<MemberCourse> MemberCourses { get; set; } = new List<MemberCourse>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Trainer? Trainer { get; set; }

    public virtual ICollection<TrainerRequest> TrainerRequests { get; set; } = new List<TrainerRequest>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WeightLog> WeightLogs { get; set; } = new List<WeightLog>();
}
