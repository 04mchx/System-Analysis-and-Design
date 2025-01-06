using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public partial class user
{
    public int USER_ID { get; set; }

    public string USER_NAME { get; set; } = null!;

    public string USER_PASSWORD { get; set; } = null!;

    public string USER_EMAIL { get; set; } = null!;

    public string? USER_LOCATION { get; set; }
    public int LoginAttempts { get; set; } = 0;

    public DateTime? LockoutEndTime { get; set; }
    public virtual ICollection<favorite> favorite { get; set; } = new List<favorite>();

    public virtual ICollection<feedback> feedback { get; set; } = new List<feedback>();

    public virtual ICollection<review> review { get; set; } = new List<review>();

    public virtual ICollection<restaurant> REST { get; set; } = new List<restaurant>();
}
