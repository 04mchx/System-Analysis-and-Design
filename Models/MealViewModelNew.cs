namespace dbFinal.Models
{
    public class MealViewModelNew
    {
        public string MealName { get; set; }
        public string MealDescription { get; set; }
        public string MealIngredient { get; set; }
        public decimal MealPrice { get; set; }
        public string MealImage { get; set; }
        public int MealId { get; set; }
        public string MealAllergen { get; set; }
        public int RestId { get; set; }
        public string RestName { get; set; }
    }
}
