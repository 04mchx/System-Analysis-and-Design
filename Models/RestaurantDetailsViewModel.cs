using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dbFinal.Models
{
    public class RestaurantDetailsViewModel
    {
        public restaurant Restaurant { get; set; }
        public IEnumerable<MealViewModelNew> Meals { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalMeals { get; set; }
    }

}
