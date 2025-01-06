using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public partial class feedback
{
    public int FEEDBACK_ID { get; set; }

    public string? FEEDBACK_WORD { get; set; }

    public int? USER_ID { get; set; }

    public virtual user? USER { get; set; }
}
