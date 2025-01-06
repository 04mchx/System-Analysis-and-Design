using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public partial class favorite
{
    public int FAV_ID { get; set; }

    public string? FAV_CUISINE { get; set; }

    public string? FAV_KIND { get; set; }

    public string? FAV_ALLERGIES { get; set; }

    public int? USER_ID { get; set; }

    public virtual user? USER { get; set; }
}
