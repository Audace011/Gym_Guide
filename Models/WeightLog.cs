using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class WeightLog
{
    public int WeightLogId { get; set; }

    public int MemberId { get; set; }

    public decimal WeightKg { get; set; }

    public decimal Bmi { get; set; }

    public string Bmicategory { get; set; } = null!;

    public DateTime LoggedAt { get; set; }

    public virtual Member Member { get; set; } = null!;
}
