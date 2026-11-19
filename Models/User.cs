using System;
using System.Collections.Generic;

namespace GymGuide.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? IdentityUserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public virtual Member? Member { get; set; }

    public virtual Trainer? Trainer { get; set; }
}
