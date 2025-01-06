using System;
using System.Collections.Generic;

namespace dbFinal.Models
{
    public class RestaurantDashboardViewModel
    {
        public string RestaurantName { get; set; }
        public string Address { get; set; }
        public string OpenTime { get; set; }
        public string Phone { get; set; } // 新增電話欄位
        public string ImageUrl { get; set; } // 新增圖片欄位
        public string LogoUrl { get; set; } // 新增餐廳Logo 的 URL
        public List<MealViewModel> Meals { get; set; } = new List<MealViewModel>();
        public List<AnnouncementViewModel> Announcements { get; set; } = new List<AnnouncementViewModel>(); // 新增公告清單
    }

    public class MealViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string? MEAL_INGREDIENTS { get; set; }

        public string? MEAL_ALLERGENINFO { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AnnouncementViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } // 公告內容
        public bool IsEmergency { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
