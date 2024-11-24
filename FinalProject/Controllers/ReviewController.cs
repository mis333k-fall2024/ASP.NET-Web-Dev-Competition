using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reviews for a property
        public async Task<IActionResult> Index(int? propertyId)
        {
            if (propertyId == null)
            {
                return NotFound();
            }

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Property)
                .Where(r => r.PropertyID == propertyId && !r.DisputeStatus)
                .ToListAsync();

            return View(reviews);
        }

        // GET: Reviews/Create/5 (PropertyId)
        [Authorize]
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check if user has stayed at this property
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            bool hasStayed = await _context.Reservations
                .AnyAsync(r => r.PropertyID == id &&
                              r.CustomerID == userId &&
                              r.CheckOut < DateTime.Now);

            if (!hasStayed)
            {
                return RedirectToAction("Index", "Properties");
            }

            // Check if user has already reviewed this property
            bool hasReviewed = await _context.Reviews
                .AnyAsync(r => r.PropertyID == id && r.CustomerID == userId);

            if (hasReviewed)
            {
                return RedirectToAction("Edit", new { propertyId = id });
            }

            ViewBag.PropertyID = id;
            return View();
        }

        // POST: Reviews/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyID,Rating,ReviewText")] Review review)
        {
            if (ModelState.IsValid)
            {
                review.CustomerID = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                review.DisputeStatus = false;
                _context.Add(review);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Properties", new { id = review.PropertyID });
            }
            return View(review);
        }

        // GET: Reviews/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (review.CustomerID != userId)
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Reviews/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewID,Rating,ReviewText")] Review review)
        {
            if (id != review.ReviewID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingReview = await _context.Reviews.FindAsync(id);
                existingReview.Rating = review.Rating;
                existingReview.ReviewText = review.ReviewText;
                existingReview.DisputeStatus = false;

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Properties", new { id = existingReview.PropertyID });
            }
            return View(review);
        }

        // POST: Reviews/Dispute/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Dispute(int id, string disputeDescription)
        {
            var review = await _context.Reviews
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReviewID == id);

            if (review == null)
            {
                return NotFound();
            }

            // Verify the host owns this property
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (review.Property.Host.Id != userId)
            {
                return Forbid();
            }

            review.DisputeStatus = true;
            review.HostComments = disputeDescription;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Properties", new { id = review.PropertyID });
        }
    }
}