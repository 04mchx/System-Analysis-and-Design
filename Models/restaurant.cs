using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public partial class restaurant
{
    public int REST_ID { get; set; }

    public string REST_NAME { get; set; } = null!;

    public string? REST_ADDRESS { get; set; }

    public string? REST_BUSINESSHOURS { get; set; }

    public string? REST_MENU { get; set; }
    public string? REST_PHONE { get; set; } // 新增餐廳電話
    public string? REST_IMAGE { get; set; } // 新增餐廳圖片路徑

    public string REST_PASSWORD { get; set; } = null!;

    public virtual ICollection<meal> meal { get; set; } = new List<meal>();

    public virtual ICollection<user> USER { get; set; } = new List<user>();

    public virtual ICollection<announcement> ANNOUNCEMENTS { get; set; } = new List<announcement>();

}
