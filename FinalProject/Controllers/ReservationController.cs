using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;
using System.Text.Json;

namespace FinalProject.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private const Int32 FIRST_CONFIRMATION_NUMBER = 21901;

        public ReservationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reservations - Show user's reservations
        [Authorize]
        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .Where(r => r.CustomerID == userId)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            return View(reservations);
        }

        // GET: Reservations/Create/5 (PropertyId)
        [Authorize]
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // POST: Reservations/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyID,CheckIn,CheckOut,NumOfGuests")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Get property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

                if (property == null)
                {
                    return NotFound();
                }

                // Check for date conflicts
                bool hasConflict = await _context.Reservations
                    .AnyAsync(r => r.PropertyID == reservation.PropertyID &&
                                 r.ReservationStatus &&
                                 ((reservation.CheckIn >= r.CheckIn && reservation.CheckIn < r.CheckOut) ||
                                  (reservation.CheckOut > r.CheckIn && reservation.CheckOut <= r.CheckOut) ||
                                  (reservation.CheckIn <= r.CheckIn && reservation.CheckOut >= r.CheckOut)));

                if (hasConflict)
                {
                    ModelState.AddModelError("", "Selected dates conflict with existing reservation.");
                    return View(property);
                }

                // Set prices from property
                reservation.WeekdayPrice = property.WeekdayPrice;
                reservation.WeekendPrice = property.WeekendPrice;
                reservation.CleaningFee = property.CleaningFee;
                reservation.CustomerID = User.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Set confirmation number
                int lastConfirmationNumber = await _context.Reservations
                    .MaxAsync(r => (int?)r.ConfirmationNumber) ?? FIRST_CONFIRMATION_NUMBER - 1;
                reservation.ConfirmationNumber = lastConfirmationNumber + 1;

                // Add to cart (session) instead of direct booking
                var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();
                cart.Add(reservation);
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return RedirectToAction(nameof(Cart));
            }
            return View(await _context.Properties.FindAsync(reservation.PropertyID));
        }

        // GET: Reservations/Cart
        [Authorize]
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();
            return View(cart);
        }

        // POST: Reservations/Checkout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();

            if (!cart.Any())
            {
                return RedirectToAction(nameof(Cart));
            }

            // Final validation
            foreach (var reservation in cart)
            {
                reservation.ReservationStatus = true;
                _context.Reservations.Add(reservation);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");

            return RedirectToAction(nameof(Confirmation), new { id = cart.First().ConfirmationNumber });
        }

        // GET: Reservations/Cancel/5
        [Authorize]
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if cancellation is allowed (more than 1 day before check-in)
            if (reservation.CheckIn <= DateTime.Now.AddDays(1))
            {
                TempData["ErrorMessage"] = "Cancellation is only allowed more than 1 day before check-in.";
                return RedirectToAction(nameof(Index));
            }

            return View(reservation);
        }

        // POST: Reservations/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            reservation.ReservationStatus = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservations/Confirmation/5
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ConfirmationNumber == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }
    }

    // Extension methods for Session
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }
    }
}