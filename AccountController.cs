using EBill.Data;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EBill.Controllers
{
    public class AccountController : Controller
    {


        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // ✅ Store login info in session
                HttpContext.Session.SetString("User", username);

                return RedirectToAction("Index", "Bills");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


    }
}
