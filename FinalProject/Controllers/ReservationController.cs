using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FinalProject.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private const Int32 FIRST_CONFIRMATION_NUMBER = 21901;

        public ReservationController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Reservations - Show user's reservations
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


        if (userId == null)
        {
            return Unauthorized();
        }

            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.CustomerID == userId)
                .OrderBy(r => r.CheckIn)
                .ToListAsync();

            return View(reservations);
        }


        // GET: Reservations/Create/5 (PropertyId)
        [Authorize]
        public async Task<IActionResult> Create(int? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (currentUser != null)
            {
                ViewBag.Customer = currentUser;  // Pass Customer ID to the View

            }

            if (id == null)
            {
                return NotFound();
            }

            // Fetch the property details
            var property = await _context.Properties
                .Include(p => p.Category) // Include related data if necessary
                .Include(p => p.Reservations)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            var reservedDates = _context.Reservations
                .Where(r => r.PropertyID == id && r.ReservationStatus == true) // Include only active reservations
                .Select(r => new { start = r.CheckIn, end = r.CheckOut })
                .ToList();

            var unavailabilityDates = _context.Unavailabilities
                .Where(u => u.PropertyID == id)
                .Select(u => new { start = u.Date, end = u.Date })
                .ToList();

            ViewBag.ReservedDates = reservedDates;
            ViewBag.UnavailabilityDates = unavailabilityDates;

            // Create a Reservation model to pass to the view
            var reservation = new Reservation
            {
                PropertyID = property.PropertyID, // Populate PropertyID
                Property = property,             // Include property details
                CustomerID = currentUser.Id      // Set the current user as the customer
            };



            return View(reservation);
        }


        // POST: Reservations/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyID,CheckIn,CheckOut,NumOfGuests")] Reservation reservation)
        {
            // Remove the properties that are not needed for the reservation
            ModelState.Remove("CustomerID");
            ModelState.Remove("Property");
            ModelState.Remove("Customer");

            if (ModelState.IsValid)
            {
                // Fetch the property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

                if (property == null)
                {
                    return NotFound();
                }

                // Assign current user details
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();


                reservation.Customer = currentUser;
                reservation.CustomerID = currentUser.Id;
                reservation.Property = property;

                reservation.WeekdayPrice = property.WeekdayPrice;
                reservation.WeekendPrice = property.WeekendPrice;
                reservation.CleaningFee = property.CleaningFee;
                //reservation.CustomerID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


                var overlappingReservations = await _context.Reservations
                    .Where(r => r.CustomerID == currentUser.Id) // Only for the current user
                    .Where(r => r.ReservationStatus == true)
                    .Where(r => r.CheckOut > reservation.CheckIn && r.CheckIn < reservation.CheckOut) // Overlapping dates
                    .ToListAsync();


                if (overlappingReservations.Any())
                {
                    TempData["ErrorMessage"] = "The selected dates overlap with a reservation you already made. Please choose different dates.";
                    return View(reservation);
                }

                var unavailableDates = await _context.Unavailabilities
                    .Where(u => u.PropertyID == reservation.PropertyID)
                    .Where(u => u.Date >= reservation.CheckIn && u.Date < reservation.CheckOut)
                    .ToListAsync();

                if (unavailableDates.Any())
                {
                    TempData["ErrorMessage"] = "The selected dates include unavailable dates. Please choose different dates.";
                    return View(reservation);
                }

                    // Set confirmation number
                    int lastConfirmationNumber = await _context.Reservations
                    .MaxAsync(r => (int?)r.ConfirmationNumber) ?? 9999;
                reservation.ConfirmationNumber = lastConfirmationNumber + 1;

                // Add to cart (session)
                var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();
                cart.Add(reservation);
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return RedirectToAction(nameof(Cart));
            }

            // Fetch the property details to populate the view again
            reservation.Property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

            if (reservation.Property == null)
            {
                return NotFound();
            }

            return View(reservation);
        }



        // GET: Reservations/Cart
        private const string CartSessionKey = "Cart";

        
        [Authorize]
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart); // Pass the cart to the view
        }

        private List<Reservation> GetCart()
        {
            return HttpContext.Session.GetObjectFromJson<List<Reservation>>(CartSessionKey) ?? new List<Reservation>();
        }

        // Helper method: Save the cart to the session
        private void SaveCart(List<Reservation> cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        // POST: Reservations/DeleteFromCart
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFromCart(int confirmationNumber)
        {
            // Retrieve the cart from the session
            var cart = GetCart();

            // Find the reservation with the matching confirmation number
            var reservationToRemove = cart.FirstOrDefault(r => r.ConfirmationNumber == confirmationNumber);
            if (reservationToRemove != null)
            {
                cart.Remove(reservationToRemove); // Remove the reservation from the cart
                SaveCart(cart); // Save the updated cart back to the session
            }

            // Redirect to the Cart view
            return RedirectToAction(nameof(Cart));
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

            // Check for overlaps within the cart itself
            for (int i = 0; i < cart.Count; i++)
            {
                for (int j = i + 1; j < cart.Count; j++)
                {
                    if (cart[i].CheckOut > cart[j].CheckIn && cart[i].CheckIn < cart[j].CheckOut)
                    {
                        TempData["ErrorMessage"] = "The selected dates for your stays overlap. Please modify your cart.";
                        return RedirectToAction(nameof(Cart));
                    }
                }
            }

            decimal stayprice = 0;
            decimal staydiscounted = 0;
            decimal cleaningFees = 0;
            decimal subtotal = 0;
            const decimal TAX_RATE = 0.07m;
            decimal tax = 0;
            decimal totaltax = 0;
            decimal totaldiscountamount = 0;
            decimal reservationsubtotal = 0;
            decimal grandTotal = 0;
            decimal subafterdis = 0;

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            try
            {
                foreach (var reservation in cart)
                {
                    decimal discountamount = 0;

                    // Check for existing reservation
                    bool exists = await _context.Reservations
                        .AnyAsync(r => r.ConfirmationNumber == reservation.ConfirmationNumber);

                    if (exists)
                    {
                        ModelState.AddModelError("",
                            $"Reservation with Confirmation Number {reservation.ConfirmationNumber} already exists.");
                        return RedirectToAction(nameof(Cart));
                    }

                    // Fetch the property once and reuse it
                    var property = await _context.Properties
                        .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

                    if (property == null)
                    {
                        TempData["ErrorMessage"] = $"Property with ID {reservation.PropertyID} not found.";
                        return RedirectToAction(nameof(Cart));
                    }

                    var nights = (reservation.CheckOut - reservation.CheckIn).Days;
                    var weekdayNights = Enumerable.Range(0, nights)
                        .Count(offset => !new[] { DayOfWeek.Friday, DayOfWeek.Saturday }
                        .Contains(reservation.CheckIn.AddDays(offset).DayOfWeek));
                    var weekendNights = nights - weekdayNights;

                    decimal reservationStayPrice = (weekdayNights * property.WeekdayPrice) +
                        (weekendNights * property.WeekendPrice);

                    if (nights >= property.MinNightsForDiscount)
                    {
                        decimal discountRate = property.DiscountRate / 100m ?? 0;
                        discountamount = reservationStayPrice * discountRate;
                    }

                    // Create new reservation entity
                    var newReservation = new Reservation
                    {
                        CustomerID = currentUser.Id,
                        PropertyID = property.PropertyID,
                        CheckIn = reservation.CheckIn,
                        CheckOut = reservation.CheckOut,
                        NumOfGuests = reservation.NumOfGuests,
                        WeekdayPrice = property.WeekdayPrice,
                        WeekendPrice = property.WeekendPrice,
                        CleaningFee = property.CleaningFee,
                        ConfirmationNumber = reservation.ConfirmationNumber,
                        ReservationStatus = true
                    };

                    _context.Reservations.Add(newReservation);

                    // Update totals
                    stayprice += reservationStayPrice;
                    totaldiscountamount += discountamount;
                    subtotal += reservationStayPrice;
                    cleaningFees += property.CleaningFee;
                    staydiscounted = subtotal - totaldiscountamount;
                    tax = (reservationStayPrice - discountamount + property.CleaningFee) * TAX_RATE;
                    reservationsubtotal = reservationStayPrice - discountamount + property.CleaningFee;
                    subafterdis = staydiscounted + cleaningFees;
                    totaltax += tax;
                    grandTotal = subtotal - totaldiscountamount + cleaningFees + totaltax;
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Cart");

                // Set TempData values
                TempData["Subtotal"] = subtotal.ToString("C");
                if (totaldiscountamount > 0)
                {
                    TempData["TotalDiscount"] = totaldiscountamount.ToString("C");
                }
                TempData["PriceAfterDis"] = staydiscounted.ToString("C");
                TempData["CleaningFee"] = cleaningFees.ToString("C");
                TempData["SubAfterDis"] = subafterdis.ToString("C");
                TempData["Tax"] = tax.ToString("C");
                TempData["GrandTotal"] = grandTotal.ToString("C");

                return RedirectToAction(nameof(Confirmation), new { id = cart.First().ConfirmationNumber });
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["ErrorMessage"] = "An error occurred while processing your reservation.";
                return RedirectToAction(nameof(Cart));
            }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                TempData["InfoMessage"] = "Reservation not found.";
                return RedirectToAction(nameof(Index));
            }

            // Mark the reservation as cancelled
            reservation.ReservationStatus = false; // Assuming false indicates cancellation
            _context.Reservations.Update(reservation);

            // Clear unavailable dates linked to the reservation
            var unavailableDates = await _context.Unavailabilities
                .Where(u => u.PropertyID == reservation.PropertyID)
                .Where(u => u.Date >= reservation.CheckIn && u.Date < reservation.CheckOut)
                .ToListAsync();

            if (unavailableDates.Any())
            {
                _context.Unavailabilities.RemoveRange(unavailableDates);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservation has been canceled successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while canceling the reservation. Please try again.";
            }

            // Redirect back to the index page to show all reservations
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservations/Confirmation/5
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the customer's most recent reservations that share the same confirmation time
            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .Where(r => r.ConfirmationNumber == id)
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound();
            }

            ViewBag.Reservations = reservations;
            return View(reservations);
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