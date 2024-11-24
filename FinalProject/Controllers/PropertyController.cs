using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Controllers
{
    public class PropertyController : Controller
    {
        private readonly AppDbContext _context;

        public PropertyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Properties/
        public async Task<IActionResult> Index()
        {
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.AdminApproved)
                .ToListAsync();

            return View(properties);
        }

        // GET: /Properties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // GET: /Properties/Search
        public async Task<IActionResult> Search(string location, DateTime? checkIn,
            DateTime? checkOut, int? guests, int? categoryId, decimal? minPrice,
            decimal? maxPrice, int? minBedrooms, int? minBathrooms,
            decimal? minRating, bool? petsAllowed, bool? freeParking)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.AdminApproved);

            // Apply search filters
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(p =>
                    p.City.Contains(location) ||
                    p.State.Contains(location) ||
                    p.PropertyName.Contains(location));
            }

            if (guests.HasValue)
            {
                query = query.Where(p => p.GuestsAllowed >= guests.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Category.CategoryID == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice <= maxPrice.Value);
            }

            if (minBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms >= minBedrooms.Value);
            }

            if (minBathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms >= minBathrooms.Value);
            }

            if (petsAllowed.HasValue)
            {
                query = query.Where(p => p.PetsAllowed == petsAllowed.Value);
            }

            if (freeParking.HasValue)
            {
                query = query.Where(p => p.FreeParking == freeParking.Value);
            }

            // Check availability if dates are provided
            if (checkIn.HasValue && checkOut.HasValue)
            {
                query = query.Where(p => !p.Reservations.Any(r =>
                    r.ReservationStatus && // Only consider active reservations
                    ((checkIn >= r.CheckIn && checkIn < r.CheckOut) || // Check-in during existing reservation
                     (checkOut > r.CheckIn && checkOut <= r.CheckOut) || // Check-out during existing reservation
                     (checkIn <= r.CheckIn && checkOut >= r.CheckOut)))); // Existing reservation within requested dates
            }

            // Get categories for search form
            ViewBag.Categories = await _context.Categories.ToListAsync();

            var properties = await query.ToListAsync();

            // Display record count
            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.FilteredCount = properties.Count;

            return View(properties);
        }

        // GET: /Properties/AdvancedSearch
        public async Task<IActionResult> AdvancedSearch()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }
    }
}