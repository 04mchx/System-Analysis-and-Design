using Microsoft.AspNetCore.Mvc;
using dbFinal.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;
using System.Text.RegularExpressions;

namespace dbFinal.Controllers
{
    public class SearchController : Controller
    {
        private readonly db_testContext _context;

        public SearchController(db_testContext context)
        {
            _context = context;
        }

        // GET: searchMeal
        [HttpGet]
        public IActionResult SearchMeal()
        {
            // 隨機選擇三個餐點
            var randomMeals = _context.meal
                .OrderBy(m => Guid.NewGuid()) // 使用 Guid.NewGuid() 隨機排序
                .Take(3) // 取三筆資料
                .Select(m => new
                {
                    MealId = m.MEAL_ID,
                    MealName = m.MEAL_NAME,
                    MealImage = m.MEAL_IMAGE ?? "/path/to/default/image.jpg" // 預設圖片
                })
                .ToList();

            ViewBag.RandomMeals = randomMeals; // 傳遞隨機餐點至 View
            return View();
        }

        private bool ContainsAnyTags(string description, List<string> tags)
        {
            if (string.IsNullOrEmpty(description)) return false;
            return tags.Any(tag => description.Contains(tag));
        }

        [HttpGet]
        [HttpPost]
        public IActionResult SearchMealResult(string searchText, List<string> nutritionTags, int page = 1, int pageSize = 5)
        {
            // 設置 TempData 保存搜索條件
            TempData["LastSearchText"] = searchText;
            TempData["LastNutritionTags"] = nutritionTags ?? new List<string>();

            // Fetch meals and handle potential NULL values in the fields
            var meals = _context.meal.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                meals = meals.Where(m => (m.MEAL_NAME ?? "").Contains(searchText) || (m.MEAL_DESCRIPTION ?? "").Contains(searchText));
            }

            if (nutritionTags != null && nutritionTags.Any())
            {
                meals = meals.AsEnumerable().Where(m => ContainsAnyTags(m.MEAL_DESCRIPTION, nutritionTags)).AsQueryable();
            }

            var totalMeals = meals.Count();
            var totalPages = (int)Math.Ceiling(totalMeals / (double)pageSize);

            var paginatedMeals = meals
                .Select(m => new MealViewModelNew
                {
                    MealId = m.MEAL_ID, // Map the ID from the database
                    MealName = m.MEAL_NAME ?? "N/A", // Default to "N/A" if MEAL_NAME is NULL
                    MealDescription = m.MEAL_DESCRIPTION ?? "No description available", // Default to "No description available"
                    MealPrice = m.MEAL_PRICE,
                    MealIngredient = m.MEAL_INGREDIENTS ?? "No ingredient available",
                    MealImage = m.MEAL_IMAGE ?? "/path/to/default/image.jpg" // Default image path if MEAL_IMAGE is NULL
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (!paginatedMeals.Any())
            {
                return RedirectToAction("SearchMealNoResult");
            }

            ViewBag.TotalMeals = totalMeals;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(paginatedMeals);
        }

        // GET: searchMealNoResult
        [HttpGet]
        public IActionResult SearchMealNoResult()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MealDetail(int mealId)
        {
            // 使用聯結查詢來獲取餐點和餐廳名稱
            var meal = (from m in _context.meal
                        join r in _context.restaurant on m.REST_ID equals r.REST_ID
                        where m.MEAL_ID == mealId
                        select new
                        {
                            m.MEAL_ID,
                            m.MEAL_NAME,
                            m.MEAL_DESCRIPTION,
                            m.MEAL_INGREDIENTS,
                            m.MEAL_PRICE,
                            m.MEAL_IMAGE,
                            m.REST_ID,
                            RestName = r.REST_NAME // 獲取餐廳名稱
                        }).FirstOrDefault();

            if (meal == null)
            {
                return NotFound("The meal does not exist.");
            }

            var mealDetail = new MealViewModelNew
            {
                MealId = meal.MEAL_ID,
                MealName = meal.MEAL_NAME ?? "N/A", // 預設名稱
                MealDescription = meal.MEAL_DESCRIPTION ?? "Description not available", // 預設描述
                MealPrice = meal.MEAL_PRICE,
                MealIngredient = meal.MEAL_INGREDIENTS ?? "No ingredient available",
                MealImage = !string.IsNullOrEmpty(meal.MEAL_IMAGE) ? meal.MEAL_IMAGE : "/path/to/default/image.jpg", // 預設圖片
                RestId = meal.REST_ID ?? 0, // 處理 null REST_ID
                RestName = meal.RestName ?? "Unknown Restaurant" // 處理 null 餐廳名稱
            };

            // 保存返回的搜索參數到 ViewData
            ViewData["LastSearchText"] = TempData["LastSearchText"] ?? "";
            ViewData["LastNutritionTags"] = TempData["LastNutritionTags"] ?? new List<string>();

            return View(mealDetail);
        }

        [HttpGet]
        public IActionResult RateMeal(int mealId)
        {
            // 使用聯結查詢來獲取餐點和餐廳名稱
            var meal = (from m in _context.meal
                        join r in _context.restaurant on m.REST_ID equals r.REST_ID
                        where m.MEAL_ID == mealId
                        select new MealViewModelNew
                        {
                            MealId = m.MEAL_ID,
                            MealName = m.MEAL_NAME ?? "N/A", // 預設名稱
                            MealDescription = m.MEAL_DESCRIPTION ?? "Description not available", // 預設描述
                            MealPrice = m.MEAL_PRICE,
                            MealIngredient = m.MEAL_INGREDIENTS ?? "No ingredient available",
                            MealImage = !string.IsNullOrEmpty(m.MEAL_IMAGE) ? m.MEAL_IMAGE : "/path/to/default/image.jpg", // 預設圖片
                            RestId = m.REST_ID ?? 0, // 處理 null REST_ID
                            RestName = r.REST_NAME ?? "Unknown Restaurant" // 處理 null 餐廳名稱
                        }).FirstOrDefault();

            if (meal == null)
            {
                return NotFound("The meal does not exist.");
            }

            return View(meal);
        }
        
        [HttpPost]
        public IActionResult SubmitRating(int mealId, int rating)
        {
            // 檢查用戶是否登入
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["Error"] = "您的登入已過期，請重新登入後再進行操作。";
                return RedirectToAction("UserLogin", "User");
            }

            // 評分範圍檢查
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "評分無效，請選擇 1 到 5 之間的評分。";
                return RedirectToAction("RateMeal", new { mealId });
            }

            // 保存評分（允許多次評分）
            var review = new review
            {
                USER_ID = userId.Value,
                MEAL_ID = mealId,
                REVIEW_RATING = rating,
                REVIEW_DATE = DateTime.Now
            };

            _context.review.Add(review);
            _context.SaveChanges();

            TempData["Success"] = "評分成功！";
            return RedirectToAction("ViewReview", "User");
        }

        // 顯示搜尋頁面
        [HttpGet]
        public IActionResult SearchRest()
        {
            return View();
        }

        [HttpGet]
        [HttpPost]
        public IActionResult SearchRestResult(string searchText)
        {
            var restaurants = _context.restaurant
                .Where(r => r.REST_NAME.Contains(searchText) || r.REST_ADDRESS.Contains(searchText))
                .Select(r => new RestaurantViewModelNew
                {
                    REST_ID = r.REST_ID,
                    REST_NAME = r.REST_NAME,
                    REST_ADDRESS = r.REST_ADDRESS,
                    REST_IMAGE = string.IsNullOrEmpty(r.REST_IMAGE) ? "/path/to/default/image.jpg" : r.REST_IMAGE
                })
                .ToList();

            if (!restaurants.Any())
            {
                return RedirectToAction("SearchRestNoResult");
            }

            return View(restaurants);
        }

        // 沒有搜尋結果
        [HttpGet]
        public IActionResult SearchRestNoResult()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RestDetail(int restId, int page = 1, int pageSize = 3, string searchText = "")
        {
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == restId);
            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            restaurant.REST_IMAGE = string.IsNullOrEmpty(restaurant.REST_IMAGE) ? "/path/to/default/logo.jpg" : restaurant.REST_IMAGE;
            var meals = _context.meal
                .Where(m => m.REST_ID == restId)
                .Select(m => new MealViewModelNew
                {
                    MealId = m.MEAL_ID,
                    MealName = m.MEAL_NAME ?? "N/A",
                    MealDescription = m.MEAL_DESCRIPTION ?? "No description available",
                    MealPrice = m.MEAL_PRICE,
                    MealIngredient = m.MEAL_INGREDIENTS ?? "No ingredient available",
                    MealAllergen = m.MEAL_ALLERGENINFO ?? "No allergen information",
                    MealImage = m.MEAL_IMAGE ?? "/path/to/default/image.jpg"
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalMeals = _context.meal.Count(m => m.REST_ID == restId);
            var totalPages = (int)Math.Ceiling((double)totalMeals / pageSize);

            var viewModel = new RestaurantDetailsViewModel
            {
                Restaurant = restaurant,
                Meals = meals,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalMeals = totalMeals
            };

            ViewData["SearchText"] = searchText;

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult RestDetail1(int restId, int page = 1, int pageSize = 3, string searchText = "")
        {
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == restId);
            if (restaurant == null)
            {
                return NotFound("Restaurant not found.");
            }

            restaurant.REST_IMAGE = string.IsNullOrEmpty(restaurant.REST_IMAGE) ? "/path/to/default/logo.jpg" : restaurant.REST_IMAGE;
            var meals = _context.meal
                .Where(m => m.REST_ID == restId)
                .Select(m => new MealViewModelNew
                {
                    MealId = m.MEAL_ID,
                    MealName = m.MEAL_NAME ?? "N/A",
                    MealDescription = m.MEAL_DESCRIPTION ?? "No description available",
                    MealPrice = m.MEAL_PRICE,
                    MealIngredient = m.MEAL_INGREDIENTS ?? "No ingredient available",
                    MealAllergen = m.MEAL_ALLERGENINFO ?? "No allergen information",
                    MealImage = m.MEAL_IMAGE ?? "/path/to/default/image.jpg"
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalMeals = _context.meal.Count(m => m.REST_ID == restId);
            var totalPages = (int)Math.Ceiling((double)totalMeals / pageSize);

            var viewModel = new RestaurantDetailsViewModel
            {
                Restaurant = restaurant,
                Meals = meals,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalMeals = totalMeals
            };

            ViewData["SearchText"] = searchText;

            return View(viewModel);
        }
    }
}
