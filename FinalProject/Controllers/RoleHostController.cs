using FinalProject.DAL;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    public class RoleHostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public RoleHostController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Host")]
        public ActionResult ViewReviews(int hostId)
        {

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (userId == null)
            {
                return RedirectToAction("Reports", "Account");
            }

            var propertyIds = _context.Properties
                .Where(p => p.Host.Id == userId)
                .Select(p => p. PropertyID)
                .ToList();
            if (propertyIds ==  null)
            {
                return RedirectToAction("Register", "Account");
            }
            var reviews = _context.Reviews
            .Where(r => propertyIds.Contains(r.PropertyID))
            .ToList();

            

            return View(reviews);
        }

        [Authorize(Roles = "Host")]
        [HttpPost]
        public IActionResult DisputeReview(int reviewId, string reason)
        {
            try
            {
                // lowkey i need to get the ones without space or null or whitespace
                var review = _context.Reviews.FirstOrDefault(r => r.ReviewID == reviewId);
                if (review == null)
                {
                    TempData["ErrorMessage"] = "Review not found.";
                    return RedirectToAction("ViewReviews");
                }

                // Update for reqs in doc
                review.DisputeStatus = DisputeStatus.Disputed; 
                review.HostComments = reason;

                
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Review disputed successfully, check back here soon to see Host Desicion.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while disputing the review.";
            }

            return RedirectToAction("ViewReviews");
        }

        public IActionResult cancelReservation(int id)
        {

            return View();
        }


        [HttpGet]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> ManageReservations()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Retrieve all reservations for properties owned by the host
            var reservations = await _context.Reservations
                .Include(r => r.Property) // Ensure Property navigation property is loaded
                .Where(r => r.Property.Host.Id == userId && r.ReservationStatus == true)
                .ToListAsync();

            if (!reservations.Any())
            {
                TempData["InfoMessage"] = "No reservations found for your properties.";
            }

            return View(reservations);
        }

        [HttpPost]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> CancelReservation(int reservationId)
        {
            // Fetch the reservation from the database
            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null)
            {
                TempData["InfoMessage"] = "Reservation not found.";
                return RedirectToAction("ManageReservations");
            }

            // Check for day off
            if (DateTime.UtcNow.Date < reservation.CheckIn.AddDays(-1).Date)
            {

                reservation.ReservationStatus = false; 
                _context.Reservations.Update(reservation);
                await _context.SaveChangesAsync();

                TempData["InfoMessage"] = "The reservation has been successfully canceled.";
            }
            else
            {
                TempData["InfoMessage"] = "It's too late to cancel this reservation.";
            }

            return RedirectToAction("ManageReservations");
        }

        [Authorize(Roles = "Host")]
        [HttpGet]
        public IActionResult UnvDates(int id)
        {
            var model = new Unavailability
            {
                PropertyID = id,
                Date = DateTime.Now // Default date
            };

            return View(model);
        }


        [Authorize(Roles = "Host")]
        [HttpPost]
        public IActionResult UnvDates(Unavailability model)
        {
            ModelState.Remove("Property");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); 
            }

            // check this
            model.Property = _context.Properties.FirstOrDefault(p => p.PropertyID == model.PropertyID);

            _context.Unavailabilities.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Reports", "Account"); 
        }

        [Authorize(Roles = "Host")]
        public IActionResult UnvDetails(int id)
        {
            var property = _context.Properties
                .Include(p => p.UnavailableDates) 
                .FirstOrDefault(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }


    }
}
