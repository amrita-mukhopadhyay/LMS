using Microsoft.AspNetCore.Mvc;

namespace LearningManagementSystem.Controllers
{
  
        public class HelpController : Controller
        {
            public IActionResult Index()
            {
                
                var faqs = new List<(string Q, string A)>
            {
                ("How do I reset my password?", "Go to settings → Security → Reset Password."),
                ("How can I contact support?", "You can reach us through the Contact form in the Help Center."),
                ("Where can I view billing details?", "Visit Billing section in your account dashboard.")
            };

                ViewBag.FAQs = faqs;
                return View();
            }

            public IActionResult Article(int id)
            {
                
                return View();
            }
        }
    }
