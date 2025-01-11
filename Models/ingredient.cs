using System;
using System.Collections.Generic;

namespace dbFinal.Models;

//P3
public partial class ingredient
{
    public int ING_ID { get; set; }

    public string ING_NAME { get; set; } = null!;

    public string? ING_ALLERGENINFO { get; set; }

    public virtual ICollection<meal> MEAL { get; set; } = new List<meal>();
}
