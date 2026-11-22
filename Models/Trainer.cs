using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class Trainer
{
    public int TrainerId { get; set; }

    public int UserId { get; set; }

    public string Specialization { get; set; } = null!;

    public int MaxClients { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();

    public virtual ICollection<TrainerRequest> TrainerRequests { get; set; } = new List<TrainerRequest>();

    public virtual User User { get; set; } = null!;
}
