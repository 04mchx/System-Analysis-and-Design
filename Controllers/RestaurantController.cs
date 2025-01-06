using Microsoft.AspNetCore.Mvc;
using dbFinal.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;


namespace dbFinal.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly db_testContext _context;
        public RestaurantController(db_testContext context)
        {
            _context = context;
        }

        // 批量加密資料庫中的密碼
        public void EncryptPasswords()
        {
            // 從資料庫中取得所有餐廳記錄
            var restaurants = _context.restaurant.ToList();

            foreach (var restaurant in restaurants)
            {
                // 如果密碼已加密，略過
                if (restaurant.REST_PASSWORD.StartsWith("$2a$"))
                {
                    continue;
                }

                // 對明文密碼進行加密
                string plainPassword = restaurant.REST_PASSWORD;
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

                // 更新加密後的密碼到資料庫
                restaurant.REST_PASSWORD = hashedPassword;
            }

            // 儲存更改
            _context.SaveChanges();
            Console.WriteLine("所有密碼已加密完成！");
        }

        public IActionResult EncryptAllPasswords()
        {
            EncryptPasswords(); // 呼叫批量加密方法
            return Content("所有密碼已成功加密！");
        }

        [HttpGet]
        public IActionResult RestLogin()
        {
            return View(); // 顯示登入頁面
        }

        [HttpPost]
        public IActionResult RestAuthenticate(int Rid, string password)
        {
            // 查詢該店家
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == Rid);
            if (restaurant == null)
            {
                ViewBag.ErrorMessage = "帳號不存在，請先註冊。";
                return View("RestLogin");
            }

            // 驗證密碼
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, restaurant.REST_PASSWORD);
            if (!isPasswordValid)
            {
                ViewBag.ErrorMessage = "密碼錯誤，請再試一次。";
                return View("RestLogin");
            }

            // 登入成功，將店家 ID 存入 Session
            HttpContext.Session.SetInt32("RestId", Rid);

            // 跳轉到 Dashboard
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // 清除 Session
            HttpContext.Session.Clear();
            return RedirectToAction("RestLogin");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(); // 顯示註冊頁面
        }

        [HttpPost]
        public IActionResult Register(string RestName, string RestPassword, bool? AgreeToTerms)
        {
            if (AgreeToTerms != true)
            {
                ViewBag.ErrorMessage = "請先閱讀並接受使用者條款和隱私政策！";
                return View();
            }

            // 檢查餐廳名稱是否已存在
            if (_context.restaurant.Any(r => r.REST_NAME == RestName))
            {
                ViewBag.ErrorMessage = "餐廳名稱已存在，請選擇其他名稱！";
                return View();
            }

            // 確保密碼不為空
            if (string.IsNullOrEmpty(RestPassword))
            {
                ViewBag.ErrorMessage = "密碼不可為空！";
                return View();
            }

            // 加密密碼
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(RestPassword);

            // 生成唯一的餐廳 ID
            int newRestId = _context.restaurant.Any()
                ? _context.restaurant.Max(r => r.REST_ID) + 1
                : 1;

            // 建立新的餐廳物件
            var newRestaurant = new restaurant
            {
                REST_ID = newRestId,
                REST_NAME = RestName,
                REST_PASSWORD = hashedPassword,
                REST_ADDRESS = "未設定",
                REST_BUSINESSHOURS = "未設定",
                REST_MENU = "未設定"
            };

            // 保存到資料庫
            _context.restaurant.Add(newRestaurant);
            _context.SaveChanges();

            // 將成功訊息和餐廳 ID 傳遞給 View
            ViewBag.SuccessMessage = $"註冊成功！您的餐廳 ID 為 {newRestId}，請記住並用於登入。";
            return View(); // 返回註冊頁面
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            TempData["AgreeToTerms"] = true;
            return View();
        }
   
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(int Rid, string RestName, string NewPassword)
        {
            // 查詢餐廳是否存在
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == Rid && r.REST_NAME == RestName);
            if (restaurant == null)
            {
                ViewBag.ErrorMessage = "找不到相符的餐廳 ID 或名稱。";
                return View();
            }

            // 加密新密碼
            restaurant.REST_PASSWORD = BCrypt.Net.BCrypt.HashPassword(NewPassword);

            // 保存更改
            _context.SaveChanges();

            ViewBag.SuccessMessage = "密碼已成功重置，請返回登入頁面。";
            return View();
        }

        [HttpGet]
        public IActionResult RetrieveRestID()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RetrieveRestID(string RestName)
        {
            // 查詢餐廳名稱對應的 ID
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_NAME == RestName);
            if (restaurant == null)
            {
                ViewBag.ErrorMessage = "找不到相符的餐廳名稱。";
                return View();
            }

            ViewBag.SuccessMessage = $"您的餐廳 ID 為：{restaurant.REST_ID}";
            return View();
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            // 從 Session 獲取餐廳 ID
            var restId = HttpContext.Session.GetInt32("RestId");
            if (restId == null)
            {
                return RedirectToAction("RestLogin");
            }

            // 查詢餐廳資料
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == restId);
            if (restaurant == null)
            {
                return NotFound("餐廳不存在！");
            }

            // 查詢餐點資料，處理 NULL 值
            var meals = _context.meal
                .Where(m => m.REST_ID == restId)
                .Select(m => new MealViewModel
                {
                    Id = m.MEAL_ID,
                    Name = m.MEAL_NAME ?? "未命名餐點",
                    Price = m.MEAL_PRICE,
                    Description = m.MEAL_DESCRIPTION ?? "描述未提供",
                    ImageUrl = m.MEAL_IMAGE ?? "/uploads/default-image.jpg"
                })
                .ToList();

            // 查詢公告資訊
            var announcements = _context.announcement
                .Where(a => a.REST_ID == restId)
                .OrderByDescending(a => a.ANNO_CREATED_AT)
                .Select(a => new AnnouncementViewModel
                {
                    Id = a.ANNO_ID,
                    Title = a.ANNO_TITLE,
                    IsEmergency = a.ANNO_IS_EMERGENCY,
                    CreatedAt = a.ANNO_CREATED_AT
                })
                .ToList();

            // 構造 ViewModel
            var viewModel = new RestaurantDashboardViewModel
            {
                RestaurantName = restaurant.REST_NAME,
                Address = restaurant.REST_ADDRESS ?? "地址未提供",
                OpenTime = restaurant.REST_BUSINESSHOURS ?? "營業時間未設定",
                Phone = restaurant.REST_PHONE ?? "電話未提供",
                LogoUrl = restaurant.REST_IMAGE, // 確保這裡傳遞正確的圖片路徑
                Meals = meals,
                Announcements = announcements
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult EditBasicInfo()
        {
            // 從 Session 獲取餐廳 ID
            var restId = HttpContext.Session.GetInt32("RestId");
            if (restId == null)
            {
                return RedirectToAction("RestLogin"); // 如果未登入，重定向到登入頁
            }

            // 查詢餐廳資料
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == restId);
            if (restaurant == null)
            {
                return NotFound("找不到該餐廳。");
            }

            return View(restaurant); // 返回編輯頁面
        }

        [HttpPost]
        public IActionResult EditBasicInfo(restaurant model, IFormFile imageFile)
        {
            // 從 Session 獲取餐廳 ID
            var restId = HttpContext.Session.GetInt32("RestId");
            if (restId == null)
            {
                return RedirectToAction("RestLogin"); // 未登入，重定向到登入頁
            }

            // 查詢餐廳資料
            var restaurant = _context.restaurant.FirstOrDefault(r => r.REST_ID == restId);
            if (restaurant == null)
            {
                return NotFound("找不到該餐廳。");
            }

            // 更新基本資料
            restaurant.REST_ADDRESS = model.REST_ADDRESS;
            restaurant.REST_BUSINESSHOURS = model.REST_BUSINESSHOURS;
            restaurant.REST_PHONE = model.REST_PHONE;

            // 處理圖片上傳
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "圖片大小不能超過 5MB";
                    return View(restaurant);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                restaurant.REST_IMAGE = "/uploads/" + fileName; // 保存圖片路徑
            }

            // 保存更改
            _context.SaveChanges();

            // 添加成功訊息
            TempData["SuccessMessage"] = "餐廳基本資料已成功更新！";

            // 重定向到 Dashboard
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult CreateAnno()
        {
            // 顯示新增公告頁面
            return View();
        }

        [HttpPost]
        public IActionResult CreateAnno(announcement model)
        {
            // 從 Session 中獲取餐廳 ID
            var restId = HttpContext.Session.GetInt32("RestId");
            if (restId == null)
            {
                // 如果未登入，重定向至登入頁面
                return RedirectToAction("RestLogin");
            }

            // 設置餐廳 ID
            model.REST_ID = restId.Value;

            // 設置公告的創建時間
            model.ANNO_CREATED_AT = DateTime.Now;

            // 保存公告
            try
            {
                _context.announcement.Add(model);
                _context.SaveChanges();

                // 添加成功訊息
                TempData["SuccessMessage"] = model.ANNO_IS_EMERGENCY
                    ? "緊急公告已成功新增！"
                    : "公告已成功新增！";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"新增公告失敗：{ex.Message}";
            }

            // 返回餐廳的 Dashboard 頁面
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult EditAnno(int id)
        {
            var anno = _context.announcement.FirstOrDefault(a => a.ANNO_ID == id);
            if (anno == null)
            {
                return NotFound("公告不存在！");
            }
            return View(anno);
        }

        [HttpPost]
        public IActionResult EditAnno(announcement model)
        {
            var anno = _context.announcement.FirstOrDefault(a => a.ANNO_ID == model.ANNO_ID);
            if (anno == null)
            {
                return NotFound("公告不存在！");
            }

            anno.ANNO_TITLE = model.ANNO_TITLE;
            anno.ANNO_CONTENT = model.ANNO_CONTENT;
            anno.ANNO_IS_EMERGENCY = model.ANNO_IS_EMERGENCY;

            _context.SaveChanges();

            // 設定成功訊息
            TempData["SuccessMessage"] = "公告已成功修改！";

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult DeleteAnno(int id)
        {
            var anno = _context.announcement.FirstOrDefault(a => a.ANNO_ID == id);
            if (anno == null)
            {
                return NotFound("公告不存在！");
            }

            _context.announcement.Remove(anno);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult Feedback()
        {
            return View(); // 回傳 Feedback 頁面
        }
        [HttpPost]
        public IActionResult SubmitFeedback(string FeedbackContent)
        {
            if (string.IsNullOrWhiteSpace(FeedbackContent))
            {
                // 如果输入为空，返回错误提示
                TempData["ErrorMessage"] = "請輸入您的回饋內容。";
                return RedirectToAction("Feedback");
            }

            // 模拟将反馈保存到数据库
            var feedback = new feedback
            {
                FEEDBACK_WORD = FeedbackContent,
            };

            // 假设您有一个 `_context` 数据上下文，保存到数据库
            _context.feedback.Add(feedback);
            _context.SaveChanges();

            // 成功后跳转到其他页面，例如 Dashboard
            TempData["SuccessMessage"] = "感謝您的回饋！";
            return RedirectToAction("Dashboard");
        }
    }
}