using Microsoft.AspNetCore.Mvc;
using dbFinal.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;

namespace dbFinal.Controllers
{
    public class MenuController : Controller
    {
        private readonly db_testContext _context;
        public MenuController(db_testContext context)
        {
            _context = context;
        }

        //R.e-1
        [HttpGet]
        public IActionResult ShowMenu(int page = 1, int pageSize = 8)
        {
            // 從 Session 獲取餐廳 ID
            var restId = HttpContext.Session.GetInt32("RestId");
            if (restId == null)
            {
                return RedirectToAction("RestLogin", "Restaurant");
            }

            // 查詢該餐廳的菜單
            var mealsQuery = _context.meal
                .Where(m => m.REST_ID == restId)
                .OrderBy(m => m.MEAL_NAME);

            // 計算總數和分頁
            var totalItems = mealsQuery.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var meals = mealsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 傳遞數據到視圖
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(meals); // 確保 meals 是 IEnumerable<meal> 類型
        }

        //M.fx1
        [HttpGet]
        public IActionResult CreateMeal()
        {
            return View();
        }

        //M.fx1
        [HttpPost]
        public IActionResult CreateMeal(meal model, IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(model.MEAL_NAME))
            {
                model.MEAL_NAME = "未命名餐點";
            }

            if (string.IsNullOrEmpty(model.MEAL_DESCRIPTION))
            {
                model.MEAL_DESCRIPTION = "描述未提供";
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                // 處理圖片上傳
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                model.MEAL_IMAGE = "/uploads/" + fileName;
            }
            else
            {
                // 預設圖片
                model.MEAL_IMAGE = "/uploads/default-image.jpg";
            }

            // 從 Session 獲取餐廳 ID 並分配給餐點
            model.REST_ID = HttpContext.Session.GetInt32("RestId") ?? 0;

            // 添加餐點到資料庫
            _context.meal.Add(model);
            _context.SaveChanges();

            // 跳轉至Dashboard
            return RedirectToAction("Dashboard", "Restaurant");
        }

        //M.fx2
        [HttpGet]
        public IActionResult EditMeal(int id)
        {
            // 查詢餐點資料
            var meal = _context.meal
                .Where(m => m.MEAL_ID == id)
                .Select(m => new meal
                {
                    MEAL_ID = m.MEAL_ID,
                    MEAL_NAME = m.MEAL_NAME ?? "未命名餐點",
                    MEAL_PRICE = m.MEAL_PRICE,
                    MEAL_DESCRIPTION = m.MEAL_DESCRIPTION ?? "描述未提供",
                    MEAL_INGREDIENTS = m.MEAL_INGREDIENTS ?? "無食材資訊",
                    MEAL_ALLERGENINFO = m.MEAL_ALLERGENINFO ?? "無過敏原資訊",
                    MEAL_IMAGE = m.MEAL_IMAGE ?? "/uploads/default-image.jpg"
                })
                .FirstOrDefault();

            if (meal == null)
            {
                return NotFound("餐點不存在！");
            }

            return View(meal);
        }

        //M.fx2
        [HttpPost]
        public IActionResult EditMeal(meal model, IFormFile imageFile)
        {
            var meal = _context.meal.FirstOrDefault(m => m.MEAL_ID == model.MEAL_ID);
            if (meal == null)
            {
                return NotFound("餐點不存在！");
            }

            meal.MEAL_NAME = model.MEAL_NAME;
            meal.MEAL_PRICE = model.MEAL_PRICE;
            meal.MEAL_DESCRIPTION = model.MEAL_DESCRIPTION;
            meal.MEAL_INGREDIENTS = model.MEAL_INGREDIENTS;
            meal.MEAL_ALLERGENINFO = model.MEAL_ALLERGENINFO;

            // 圖片上傳處理
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                if (!string.IsNullOrEmpty(meal.MEAL_IMAGE) && meal.MEAL_IMAGE != "/uploads/default-image.jpg")
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", meal.MEAL_IMAGE.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                meal.MEAL_IMAGE = "/uploads/" + fileName;
            }
            else
            {
                // 保留原圖片路徑
                model.MEAL_IMAGE = meal.MEAL_IMAGE;
            }

            _context.SaveChanges();

            return RedirectToAction("Dashboard", "Restaurant");
        }

        //M.fx3
        [HttpPost]
        public IActionResult DeleteMeal(int id)
        {
            var meal = _context.meal.FirstOrDefault(m => m.MEAL_ID == id);
            if (meal == null)
            {
                return NotFound("餐點不存在！");
            }

            // 刪除圖片文件
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", meal.MEAL_IMAGE.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.meal.Remove(meal);
            _context.SaveChanges();

            return RedirectToAction("Dashboard", "Restaurant");
        }
    }
}
