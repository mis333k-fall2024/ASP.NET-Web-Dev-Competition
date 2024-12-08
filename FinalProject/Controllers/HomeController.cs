using Microsoft.AspNetCore.Mvc;
using FinalProject.Models;
using FinalProject.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Home/
        public async Task<IActionResult> Index()
        {
            // Get all active and approved properties with their related data
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.IsActive)
                .ToListAsync();

            // Get total count of only approved and active properties
            ViewBag.TotalCount = await _context.Properties
                .CountAsync(p => p.PropertyStatus && p.IsActive);

            ViewBag.FilteredCount = properties.Count;

            // Get all categories for the search filters
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(properties);
        }

        // GET: /Home/
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var property = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null) return NotFound();

            return View("~/Views/Property/Details.cshtml", property);
        }

        // GET: /Home/
        public async Task<IActionResult> Search(
            string location = null,
            string propertyNumber = null,
            string state = null,
            DateTime? checkIn = null,
            DateTime? checkOut = null,
            int? guests = null,
            int[] categories = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minBedrooms = null,
            int? maxBedrooms = null,
            int? minBathrooms = null,
            int? maxBathrooms = null,
            decimal? minRating = null,
            decimal? maxRating = null,
            bool? petsAllowed = null,
            bool? freeParking = null)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Include(p => p.Reservations)
                .Include(p => p.UnavailableDates)
                .Where(p => p.PropertyStatus && p.IsActive); // Only show active and approved properties

            // Apply filters one by one
            if (!string.IsNullOrEmpty(location))
            {
                query = ApplyLocationFilter(query, location);
            }

            // Property Number filter
            if (!string.IsNullOrEmpty(propertyNumber))
            {
                query = query.Where(p => p.PropertyNumber.ToString().Contains(propertyNumber));
            }

            // State filter
            if (!string.IsNullOrEmpty(state))
            {
                query = query.Where(p => p.State.ToLower().Contains(state.ToLower()));
            }

            // Multiple Categories filter
            if (categories != null && categories.Length > 0)
            {
                query = query.Where(p => categories.Contains(p.CategoryID));
            }

            if (guests.HasValue)
            {
                query = query.Where(p => p.GuestsAllowed >= guests.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice >= minPrice.Value || p.WeekendPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice <= maxPrice.Value && p.WeekendPrice <= maxPrice.Value);
            }

            // Bedroom range
            if (minBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms >= minBedrooms.Value);
            }
            if (maxBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms <= maxBedrooms.Value);
            }

            // Bathroom range
            if (minBathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms >= minBathrooms.Value);
            }
            if (maxBathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms <= maxBathrooms.Value);
            }

            if (petsAllowed.HasValue)
            {
                query = query.Where(p => p.PetsAllowed == petsAllowed.Value);
            }

            if (freeParking.HasValue)
            {
                query = query.Where(p => p.FreeParking == freeParking.Value);
            }

            // Handle date availability
            if (checkIn.HasValue && checkOut.HasValue)
            {
                query = query.Where(p =>
                    // No overlapping reservations
                    !p.Reservations.Any(r =>
                        r.ReservationStatus == true &&
                        (
                            (checkIn >= r.CheckIn && checkIn < r.CheckOut) ||
                            (checkOut > r.CheckIn && checkOut <= r.CheckOut) ||
                            (checkIn <= r.CheckIn && checkOut >= r.CheckOut)
                        )
                    ) &&
                    // No unavailable dates
                    !p.UnavailableDates.Any(ud =>
                        ud.Date >= checkIn && ud.Date < checkOut
                    )
                );
            }

            // Apply rating filter with range
            if (minRating.HasValue || maxRating.HasValue)
            {
                query = query.Where(p =>
                    p.Reviews.Any() &&
                    (!minRating.HasValue || p.Reviews.Where(r => r.DisputeStatus != DisputeStatus.ValidDispute)
                                                  .Average(r => (decimal)r.Rating) >= minRating.Value) &&
                    (!maxRating.HasValue || p.Reviews.Where(r => r.DisputeStatus != DisputeStatus.ValidDispute)
                                                  .Average(r => (decimal)r.Rating) <= maxRating.Value));
            }

            var properties = await query.ToListAsync();

            // Set ViewBag data for the view
            ViewBag.TotalCount = await _context.Properties.CountAsync(p => p.PropertyStatus && p.IsActive);
            ViewBag.FilteredCount = properties.Count;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View("Index", properties);
        }

        private IQueryable<Property> ApplyLocationFilter(IQueryable<Property> query, string location)
        {
            var searchTerms = location.ToLower()
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .ToArray();

            return query.Where(p => searchTerms.All(term =>
                p.Street.ToLower().Contains(term) ||
                p.City.ToLower().Contains(term) ||
                p.State.ToLower().Contains(term) ||
                p.Zip.Contains(term)));
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