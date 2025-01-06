using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dbFinal.Models;

public partial class meal
{
    public int MEAL_ID { get; set; }

    [Required(ErrorMessage = "餐點名稱是必填的")]
    public string MEAL_NAME { get; set; } = null!;

    [Required(ErrorMessage = "價格是必填的")]
    [Range(0.01, double.MaxValue, ErrorMessage = "價格必須大於 0")]
    public decimal MEAL_PRICE { get; set; }

    [Required(ErrorMessage = "描述是必填的")]
    public string MEAL_DESCRIPTION { get; set; }
    public string? MEAL_INGREDIENTS { get; set; }

    public string? MEAL_ALLERGENINFO { get; set; }
    public string MEAL_IMAGE { get; set; } // 圖片路徑
    public int? REST_ID { get; set; }

    public virtual restaurant? REST { get; set; }

    public virtual ICollection<review> review { get; set; } = new List<review>();

    public virtual ICollection<ingredient> ING { get; set; } = new List<ingredient>();
}
