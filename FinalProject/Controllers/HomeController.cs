using Microsoft.AspNetCore.Mvc;
using FinalProject.Models;
using FinalProject.DAL;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Home/
        public async Task<IActionResult> Index()
        {
            // Get all active and approved properties with their related data
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.AdminApproved)
                .ToListAsync();

            // Get all categories for the search filters
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(properties);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}