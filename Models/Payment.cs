using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int MemberId { get; set; }

    public int PlanId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public DateTime PaidAt { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual MembershipPlan Plan { get; set; } = null!;
}
