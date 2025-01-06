using System;
using System.Collections.Generic;

namespace dbFinal.Models;

public class announcement
{
    public int ANNO_ID { get; set; }

    public string ANNO_TITLE { get; set; } = null!;

    public string ANNO_CONTENT { get; set; } = null!;

    public bool ANNO_IS_EMERGENCY { get; set; } // 是否為緊急公告

    public DateTime ANNO_CREATED_AT { get; set; } = DateTime.Now;

    public int REST_ID { get; set; } // 關聯餐廳

    public virtual restaurant RESTAURANT { get; set; } = null!;
}

