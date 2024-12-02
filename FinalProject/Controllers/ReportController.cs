using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private const decimal COMMISSION_RATE = 0.10m;

        public ReportController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminReport()
        {
            var viewModel = new ReportViewModel
            {
                IsAllTime = true  // Flag to indicate showing all-time data
            };

            return AdminReport(viewModel).Result;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport(ReportViewModel viewModel)
        {
            // Query all valid reservations
            var query = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.ReservationStatus == true);

            // Apply date filters if provided
            if (viewModel.StartDate.HasValue && viewModel.EndDate.HasValue)
            {
                viewModel.IsAllTime = false;  // Not showing all-time data
                query = query.Where(r => r.CheckOut >= viewModel.StartDate &&
                                       r.CheckIn <= viewModel.EndDate);
            }

            var reservations = await query.ToListAsync();

            // Calculate commission and totals
            viewModel.TotalCommission = 0;
            viewModel.TotalCompletedReservations = 0;

            foreach (var reservation in reservations)
            {
                var stayRevenue = reservation.SubTotal - reservation.CleaningFee;
                viewModel.TotalCommission += stayRevenue * COMMISSION_RATE;
                viewModel.TotalCompletedReservations++;
            }

            // Calculate average commission per reservation
            viewModel.AverageCommissionPerReservation = viewModel.TotalCompletedReservations > 0
                ? viewModel.TotalCommission / viewModel.TotalCompletedReservations
                : 0;

            // Get total number of approved properties
            viewModel.TotalProperties = await _context.Properties
                .Where(p => p.PropertyStatus == true)
                .CountAsync();

            viewModel.TotalRevenue = viewModel.TotalCommission;

            return View(viewModel);
        }

        [Authorize(Roles = "Host")]
        public IActionResult HostReport()
        {
            var viewModel = new ReportViewModel
            {
                IsAllTime = true  // Flag to indicate showing all-time data
            };

            return HostReport(viewModel).Result;
        }

        [HttpPost]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> HostReport(ReportViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);

            var query = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.Property.Host.Id == user.Id &&
                           r.ReservationStatus == true);

            if (viewModel.StartDate.HasValue && viewModel.EndDate.HasValue)
            {
                viewModel.IsAllTime = false;  // Not showing all-time data
                query = query.Where(r => r.CheckOut >= viewModel.StartDate &&
                                       r.CheckIn <= viewModel.EndDate);
            }

            var reservations = await query.ToListAsync();

            foreach (var reservation in reservations)
            {
                var propertyDetail = viewModel.PropertyDetails
                    .FirstOrDefault(p => p.PropertyID == reservation.Property.PropertyID);

                if (propertyDetail == null)
                {
                    propertyDetail = new PropertyReportDetail
                    {
                        PropertyID = reservation.Property.PropertyID,
                        PropertyAddress = $"{reservation.Property.Street}, {reservation.Property.City}, {reservation.Property.State} {reservation.Property.Zip}"
                    };
                    viewModel.PropertyDetails.Add(propertyDetail);
                }

                var stayRevenue = reservation.SubTotal - reservation.CleaningFee;
                propertyDetail.StayRevenue += stayRevenue * (1 - COMMISSION_RATE);
                propertyDetail.CleaningFees += reservation.CleaningFee;
                propertyDetail.TotalRevenue = propertyDetail.StayRevenue + propertyDetail.CleaningFees;
                propertyDetail.CompletedReservations++;
            }

            viewModel.TotalStayRevenue = viewModel.PropertyDetails.Sum(p => p.StayRevenue);
            viewModel.TotalCleaningFees = viewModel.PropertyDetails.Sum(p => p.CleaningFees);
            viewModel.TotalRevenue = viewModel.TotalStayRevenue + viewModel.TotalCleaningFees;
            viewModel.TotalCompletedReservations = viewModel.PropertyDetails.Sum(p => p.CompletedReservations);

            return View(viewModel);
        }
    }
}