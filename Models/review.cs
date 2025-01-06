using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public partial class review
{
    public int REVIEW_ID { get; set; }

    public int USER_ID { get; set; }

    public int MEAL_ID { get; set; }

    public int? REVIEW_RATING { get; set; }

    public DateTime? REVIEW_DATE { get; set; }

    public virtual meal MEAL { get; set; } = null!;

    public virtual user USER { get; set; } = null!;
}
