using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class TrainerRequest
{
    public int RequestId { get; set; }

    public int MemberId { get; set; }

    public int? TrainerId { get; set; }

    public DateTime RequestedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual Trainer? Trainer { get; set; }
}
