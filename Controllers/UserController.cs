using Microsoft.AspNetCore.Mvc;
using dbFinal.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using BCrypt.Net;
using System.Text.RegularExpressions;


namespace dbFinal.Controllers
{
    public class UserController : Controller
    {
        private readonly db_testContext _context;
        private const int MaxLoginAttempts = 3;
        private const int LockoutDurationMinutes = 5;

        public UserController(db_testContext context)
        {
            _context = context;
        }

        // 加密資料庫中的密碼
        //A.encode_password
        public void EncryptPasswords()
        {
            var users = _context.user.ToList();

            foreach (var user in users)
            {
                if (user.USER_PASSWORD.StartsWith("$2a$"))
                {
                    continue;
                }

                string plainPassword = user.USER_PASSWORD;
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                user.USER_PASSWORD = hashedPassword;
            }

            _context.SaveChanges();
            Console.WriteLine("所有密碼已加密完成！");
        }

        //A.encode_password
        public IActionResult EncryptAllPasswords()
        {
            EncryptPasswords();
            return Content("所有密碼已成功加密！");
        }

        //A.fx1
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        //A.fx1-1
        [HttpPost]
        public IActionResult UserAuthenticate(string UserName, string password)
        {
            // 驗證 userName 格式與長度
            var usernamePattern = @"^[a-zA-Z0-9._]+$";
            if (string.IsNullOrEmpty(UserName) || UserName.Length < 6 || !Regex.IsMatch(UserName, usernamePattern))
            {
                ViewBag.ErrorMessage = "使用者名稱格式錯誤。請確認格式後重試。";
                return View("UserLogin");
            }

            // 查詢使用者
            var user = _context.user.FirstOrDefault(u => u.USER_NAME == UserName);
            if (user == null)
            {
                ViewBag.ErrorMessage = "帳號不存在，請先註冊。";
                return View("UserLogin");
            }

            // 檢查帳號鎖定狀態
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.Now)
            {
                ViewBag.ErrorMessage = $"帳號已鎖定，請在 {user.LockoutEndTime.Value.ToString("HH:mm")} 後再試。";
                return View("UserLogin");
            }

            // 驗證密碼
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.USER_PASSWORD);
            if (!isPasswordValid)
            {
                user.LoginAttempts++;

                if (user.LoginAttempts >= MaxLoginAttempts)
                {
                    user.LockoutEndTime = DateTime.Now.AddMinutes(LockoutDurationMinutes);
                    ViewBag.ErrorMessage = $"密碼錯誤次數過多，帳號已鎖定 {LockoutDurationMinutes} 分鐘。";
                }
                else
                {
                    ViewBag.ErrorMessage = $"密碼錯誤，您還有 {MaxLoginAttempts - user.LoginAttempts} 次機會。";
                }

                _context.SaveChanges();
                return View("UserLogin");
            }

            // 登入成功：重設失敗次數與鎖定時間
            user.LoginAttempts = 0;
            user.LockoutEndTime = null;
            _context.SaveChanges();

            // 設置 Session 或其他登入狀態
            HttpContext.Session.SetInt32("UserId", user.USER_ID);
            HttpContext.Session.SetString("UserName", user.USER_NAME);

            return RedirectToAction("UserMainpage");
        }

        //A.fx1-Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("UserLogin");
        }

        //A.fx2
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //A.fx2
        [HttpPost]
        public IActionResult Register(string UserName, string UserPassword, string UserEmail, bool? AgreeToTerms)
        {
            Console.WriteLine($"AgreeToTerms: {AgreeToTerms}");
            // 檢查 AgreeToTerms 是否為 true
            if (AgreeToTerms != true)
            {
                ViewBag.ErrorMessage = "請閱讀並同意使用者條款與隱私政策後再進行註冊。";
                return View();
            }

            // 驗證 UserName 格式
            var userNameRegex = new Regex("^[a-zA-Z0-9._]{6,}$");
            if (string.IsNullOrEmpty(UserName) || !userNameRegex.IsMatch(UserName))
            {
                ViewBag.ErrorMessage = "使用者名稱格式錯誤，請使用至少6位的英文、數字、底線或點。";
                return View();
            }

            // 驗證 EMAIL 格式
            if (string.IsNullOrEmpty(UserEmail) || !new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(UserEmail))
            {
                ViewBag.ErrorMessage = "電子郵件格式錯誤，請輸入有效的電子郵件。";
                return View();
            }

            // 驗證 UserName 是否存在
            if (_context.user.Any(u => u.USER_NAME == UserName))
            {
                ViewBag.ErrorMessage = "使用者名稱已存在，請選擇其他名稱！";
                return View();
            }
            // 驗證 Email 是否存在
            if (_context.user.Any(u => u.USER_EMAIL == UserEmail))
            {
                ViewBag.ErrorMessage = "電子郵件已被使用，請使用其他電子郵件！";
                return View();
            }
            // 驗證密碼長度
            if (string.IsNullOrEmpty(UserPassword) || UserPassword.Length < 8)
            {
                ViewBag.ErrorMessage = "密碼長度需至少8位數！";
                return View();
            }

            // 加密密碼
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(UserPassword);

            // 生成新 USER_ID
            int newUserId = _context.user.Any() ? _context.user.Max(u => u.USER_ID) + 1 : 1;

            // 創建新 User 物件
            var newUser = new user
            {
                USER_ID = newUserId,
                USER_NAME = UserName,
                USER_PASSWORD = hashedPassword,
                USER_EMAIL = UserEmail,
                LoginAttempts = 0,
                LockoutEndTime = null
            };

            // 儲存到資料庫
            _context.user.Add(newUser);
            _context.SaveChanges();

            ViewBag.SuccessMessage = "註冊成功！請使用您的帳號登入。";
            return View();
        }

        //A1-1
        [HttpGet]
        public IActionResult TermsOfService()
        {
            TempData["AgreeToTerms"] = true;
            return View();
        }

        //A1-1
        [HttpGet]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        //A.fx1_forgetpsw
        [HttpGet]
        public IActionResult UserForgotPassword()
        {
            return View();
        }

        //A.fx1_forgetpsw
        [HttpPost]
        public IActionResult UserForgotPassword(string UserName, string NewPassword)
        {
            // 驗證使用者名稱是否存在
            var user = _context.user.FirstOrDefault(u => u.USER_NAME == UserName);
            if (user == null)
            {
                ViewBag.ErrorMessage = "使用者名稱不存在，請確認輸入正確。";
                return View();
            }

            // 驗證新密碼長度
            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8)
            {
                ViewBag.ErrorMessage = "新密碼長度需至少8位數！";
                return View();
            }

            // 加密新密碼並儲存
            user.USER_PASSWORD = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            _context.SaveChanges();

            ViewBag.SuccessMessage = "密碼已成功重置，請返回登入頁面。";
            return View();
        }

        //A4
        [HttpGet]
        public IActionResult UserMainPage()
        {
            // 從 Session 獲取用戶 ID
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("UserLogin");
            }

            var user = _context.user.FirstOrDefault(u => u.USER_ID == userId.Value);
            if (user == null)
            {
                return RedirectToAction("UserLogin");
            }

            // 獲取有公告的餐廳及所有公告
            var announcements = _context.announcement
                .OrderByDescending(a => a.ANNO_CREATED_AT) // 按時間從新到舊排序
                .Select(a => new
                {
                    RestaurantName = a.RESTAURANT.REST_NAME,
                    AnnouncementTitle = a.ANNO_TITLE // 公告標題
                })
                .ToList();

            // 隨機選擇三個餐點，並處理 NULL 值
            var randomMeals = _context.meal
                .OrderBy(m => Guid.NewGuid()) // 使用 Guid.NewGuid() 隨機排序
                .Take(3) // 取三筆資料
                .Select(m => new
                {
                    MealId = m.MEAL_ID,
                    MealName = m.MEAL_NAME,
                    MealImage = string.IsNullOrEmpty(m.MEAL_IMAGE) ? "/path/to/default/image.jpg" : m.MEAL_IMAGE // 如果沒有圖片，顯示預設圖片
                })
                .ToList();

            ViewBag.UserName = user.USER_NAME; // 傳遞使用者名稱
            ViewBag.Announcements = announcements;
            ViewBag.RandomMeals = randomMeals; // 傳遞到前端使用

            return View(user);
        }

        //A3-2
        [HttpGet]
        public IActionResult EditProfile(int? id)
        {
            // 如果沒有提供 id，從 Session 中獲取
            if (!id.HasValue)
            {
                id = HttpContext.Session.GetInt32("UserId");
                if (!id.HasValue)
                {
                    return RedirectToAction("UserLogin"); // 如果未登入，跳轉到登入頁面
                }
            }

            // 根據 ID 查詢使用者
            var user = _context.user.FirstOrDefault(u => u.USER_ID == id.Value);

            if (user == null)
            {
                return NotFound("找不到該用戶。");
            }

            return View(user);
        }

        //A3-2
        [HttpPost]
        public IActionResult EditProfile(user model, string OldPassword, string NewPassword, string ConfirmPassword)
        {
            // 從資料庫查找現有用戶
            var user = _context.user.FirstOrDefault(u => u.USER_ID == model.USER_ID);

            if (user == null)
            {
                ViewBag.ErrorMessage = "找不到該用戶。";
                return View(model);
            }

            // 驗證地址是否為必填
            if (string.IsNullOrEmpty(model.USER_LOCATION))
            {
                ViewBag.ErrorMessage = "位置為必填欄位，請選擇縣市。";
                return View(model);
            }

            // 如果要更改密碼
            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (string.IsNullOrEmpty(OldPassword) || !BCrypt.Net.BCrypt.Verify(OldPassword, user.USER_PASSWORD))
                {
                    ViewBag.ErrorMessage = "舊密碼不正確，請重新輸入。";
                    return View(model);
                }

                if (NewPassword.Length < 8)
                {
                    ViewBag.ErrorMessage = "新密碼長度需至少為8個字元，請重新輸入。";
                    return View(model);
                }

                if (NewPassword == OldPassword)
                {
                    ViewBag.ErrorMessage = "新密碼不得與舊密碼相同，請重新輸入。";
                    return View(model);
                }

                if (NewPassword != ConfirmPassword)
                {
                    ViewBag.ErrorMessage = "新密碼與確認密碼不一致，請重新輸入。";
                    return View(model);
                }

                // 加密存儲新密碼
                user.USER_PASSWORD = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            // 更新用戶基本資料
            user.USER_NAME = model.USER_NAME;
            user.USER_EMAIL = model.USER_EMAIL;
            user.USER_LOCATION = model.USER_LOCATION;

            // 保存更改
            _context.SaveChanges();

            TempData["SuccessMessage"] = "個人資料已成功更新！";
            return RedirectToAction("UserMainPage");
        }

        //A3-1
        [HttpGet]
        public IActionResult EditFavorite()
        {
            // 獲取當前用戶 ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("UserLogin"); // 如果未登入，跳轉到登入頁面
            }

            // 查詢用戶的喜好資料
            var favorite = _context.favorite.FirstOrDefault(f => f.USER_ID == userId.Value);

            // 如果用戶沒有設定過喜好，創建空的 Favorite 物件
            if (favorite == null)
            {
                favorite = new favorite
                {
                    USER_ID = userId.Value
                };
            }

            return View(favorite);
        }

        //A3-1
        [HttpPost]
        public IActionResult EditFavorite(favorite model)
        {
            // 獲取當前用戶 ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("UserLogin"); // 如果未登入，跳轉到登入頁面
            }

            // 查詢用戶的喜好資料
            var favorite = _context.favorite.FirstOrDefault(f => f.USER_ID == userId.Value);

            if (favorite == null)
            {
                // 如果用戶之前未設定過喜好，新增一條記錄
                favorite = new favorite
                {
                    USER_ID = userId.Value,
                    FAV_CUISINE = model.FAV_CUISINE,
                    FAV_KIND = model.FAV_KIND,
                    FAV_ALLERGIES = model.FAV_ALLERGIES
                };
                _context.favorite.Add(favorite);
            }
            else
            {
                // 如果用戶已設定過喜好，更新記錄
                favorite.FAV_CUISINE = model.FAV_CUISINE;
                favorite.FAV_KIND = model.FAV_KIND;
                favorite.FAV_ALLERGIES = model.FAV_ALLERGIES;
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "喜好設定已成功更新！";
            return RedirectToAction("UserMainPage");
        }

        //M3.fx4
        [HttpGet]
        public IActionResult ViewReview(int page = 1, int pageSize = 5)
        {
            // 獲取當前用戶 ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("UserLogin"); // 未登入則跳轉到登入頁面
            }

            // 查詢評分紀錄
            var reviews = _context.review
                .Where(r => r.USER_ID == userId.Value)
                .Include(r => r.MEAL)
                .ThenInclude(m => m.REST) // 加入餐廳關聯
                .OrderByDescending(r => r.REVIEW_DATE) // 根據日期排序
                .Skip((page - 1) * pageSize) // 分頁
                .Take(pageSize)
                .ToList();

            // 計算總頁數
            var totalReviews = _context.review.Count(r => r.USER_ID == userId.Value);
            var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            // 傳遞分頁資訊到 View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(reviews);
        }

        //M3.fx3
        [HttpPost]
        public IActionResult DeleteReview(int reviewId)
        {
            // 獲取當前用戶 ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("UserLogin"); // 未登入則跳轉到登入頁面
            }

            // 查詢要刪除的評分紀錄
            var review = _context.review.FirstOrDefault(r => r.REVIEW_ID == reviewId && r.USER_ID == userId.Value);
            if (review == null)
            {
                TempData["Error"] = "無法找到該評分紀錄或您無權刪除此紀錄。";
                return RedirectToAction("ViewReview");
            }

            // 刪除紀錄
            _context.review.Remove(review);
            _context.SaveChanges();

            TempData["Success"] = "評分紀錄已成功刪除。";
            return RedirectToAction("ViewReview");
        }

        //S2
        [HttpGet]
        public IActionResult Feedback()
        {
            return View(); // 回傳 Feedback 頁面
        }

        //S8-1.fx1
        [HttpPost]
        public IActionResult SubmitFeedback(string FeedbackContent)
        {
            if (string.IsNullOrEmpty(FeedbackContent))
            {
                TempData["Error"] = "回饋內容不可為空！";
                return RedirectToAction("Feedback");
            }

            // 處理回饋數據
            TempData["Success"] = "感謝您的回饋！";

            return RedirectToAction("UserMainPage");
        }
    }
}
