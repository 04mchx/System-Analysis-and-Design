using Microsoft.AspNetCore.Mvc;
using dbFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace dbFinal.Controllers
{
    public class testController : Controller
    {
        private readonly db_testContext _context;
        public testController(db_testContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> testA()
        {
            var rest = await _context.restaurant.ToListAsync();
            return View(rest);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
