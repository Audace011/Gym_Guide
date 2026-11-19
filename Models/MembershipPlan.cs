using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class MembershipPlan
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public int DurationMonths { get; set; }

    public decimal Price { get; set; }

    public bool IncludesTrainer { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
