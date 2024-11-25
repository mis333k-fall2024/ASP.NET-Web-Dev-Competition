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

        // GET: /Property/
        public async Task<IActionResult> Index()
        {
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.AdminApproved)
                .ToListAsync();

            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(properties);
        }

        // GET: /Property/Details/5
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

        // GET: /Property/Search
        public async Task<IActionResult> Search(
            string location = null,
            DateTime? checkIn = null,
            DateTime? checkOut = null,
            int? guests = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minBedrooms = null,
            int? minBathrooms = null,
            decimal? minRating = null,
            bool? petsAllowed = null,
            bool? freeParking = null)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.AdminApproved);

            // Apply search filters
            if (!string.IsNullOrEmpty(location))
            {
                location = location.ToLower().Trim();
                query = query.Where(p =>
                    p.City.ToLower().Contains(location) ||
                    p.State.ToLower().Contains(location) ||
                    (p.City.ToLower() + ", " + p.State.ToLower()).Contains(location) ||
                    p.PropertyName.ToLower().Contains(location));
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

            // Apply rating filter if specified
            if (minRating.HasValue)
            {
                query = query.Where(p =>
                    p.Reviews.Any() &&
                    p.Reviews.Where(r => !r.DisputeStatus)
                             .Average(r => (decimal)r.Rating) >= minRating.Value);
            }

            var properties = await query.ToListAsync();

            // Populate ViewBag data for the view
            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.FilteredCount = properties.Count;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Return the search view with results
            return View(properties);
        }
    }
}