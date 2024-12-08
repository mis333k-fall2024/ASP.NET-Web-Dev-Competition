using FinalProject.DAL;
using FinalProject.Models;
using FinalProject.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            // Check if user is already logged in
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel rvm)
        {
            if (!ModelState.IsValid)
            {
                return View(rvm);
            }

            // Check if user is at least 18 years old
            var age = DateTime.Today.Year - rvm.Birthday.Year;
            if (rvm.Birthday > DateTime.Today.AddYears(-age)) age--;
            if (age < 18)
            {
                ModelState.AddModelError("", "You must be at least 18 years old to register.");
                return View(rvm);
            }

            // Check if email is already in use
            var existingUser = await _userManager.FindByEmailAsync(rvm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View(rvm);
            }

            // Create the user
            AppUser newUser = new AppUser
            {
                UserName = rvm.Email,
                Email = rvm.Email,
                PhoneNumber = rvm.PhoneNumber,
                FirstName = rvm.FirstName,
                LastName = rvm.LastName,
                Address = rvm.Address,
                Birthday = rvm.Birthday
            };

            IdentityResult userResult = await _userManager.CreateAsync(newUser, rvm.Password);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(rvm);
            }

            // Check and assign role
            if (!string.IsNullOrEmpty(rvm.Role))
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Name == rvm.Role);
                if (!roleExists)
                {
                    _context.Roles.Add(new IdentityRole { Name = rvm.Role });
                    await _context.SaveChangesAsync();
                }

                IdentityResult roleResult = await _userManager.AddToRoleAsync(newUser, rvm.Role);
                if (!roleResult.Succeeded)
                {
                    // If role assignment fails, delete the user to maintain consistency
                    await _userManager.DeleteAsync(newUser);

                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(rvm);
                }
            }

            // Sign in the user
            await _signInManager.SignInAsync(newUser, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Remove the IsActive check in Login method:
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel lvm, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(lvm);
            }

            var result = await _signInManager.PasswordSignInAsync(lvm.Email, lvm.Password, lvm.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(lvm.Email);
                if (await _userManager.IsInRoleAsync(user, "Host"))
                {
                    return RedirectToAction("HostDashboard"); // Redirect to Host Dashboard
                }
                return Redirect(returnUrl ?? "/");
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(lvm);
        }

        // Logs out the user and redirects to the hosting registration page
        public async Task<IActionResult> LogoutAndRedirectToRegister()
        {
            if (User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
            }
            // Redirect to the host registration page
            return RedirectToAction("Register", "Account");
        }

        // Logs out the user and redirects to the login page
        public async Task<IActionResult> LogoutAndRedirectToLogin()
        {
            if (User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
            }
            // Redirect to the login page
            return RedirectToAction("Login", "Account");
        }

        [Authorize(Roles = "Host")]
        public async Task<IActionResult> HostDashboard()
        {


            // Assuming `User.Identity.Name` holds the current user's identifier (email, username, etc.)
            var user = _context.Users
                .Include(u => u.Properties)    // Eagerly load Properties
                .Include(u => u.Reservations)  // Eagerly load Reservations
                .Include(u => u.Reviews)       // Eagerly load Reviews
                .FirstOrDefault(u => u.Email == User.Identity.Name); // Adjust if not using Email

            if (user == null)
            {
                return NotFound("User not found");
            }
            // Get the property IDs owned by the host
            var propertyIds = await _context.Properties
                .Where(p => p.Host.Id == user.Id)
                .Select(p => p.PropertyID)
                .ToListAsync();

            
            var totalReservations = await _context.Reservations
                .CountAsync(r => propertyIds.Contains(r.PropertyID));
            
            
            var totalReviews = await _context.Reviews
                .CountAsync(r => propertyIds.Contains(r.PropertyID));


            var reservations = await _context.Reservations
            .Where(r => propertyIds.Contains(r.PropertyID))
            .ToListAsync(); 

            var totalProfit = reservations
                .Sum(r => (r.CalculateBaseTotal() * 0.90m) + r.CleaningFee);
            ViewBag.totalProfit = totalProfit;
            ViewBag.TotalReservations = totalReservations;
            ViewBag.TotalReviews = totalReviews;

            return View(user);
        }

        [AllowAnonymous]
        public async Task<IActionResult> HostInfo()
        {
            return View();
        }

        // GET: Reviews/Dispute
        [HttpGet]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Reports()
        {
            // Get the current user's ID
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Retrieve all properties owned by the host
            var properties = await _context.Properties
                .Where(p => p.Host.Id == userId)
                .Include(p => p.Reservations)
                .ToListAsync();

            return View(properties);
        }

        [Authorize(Roles = "Host")]
        [HttpGet]
        public IActionResult HostSummary()
        {
            // Return an empty model on the initial GET request
            return View(new List<HostSummaryViewModel>());
        }
        [Authorize(Roles = "Host")]
        [HttpPost]
        public async Task<IActionResult> HostSummary(DateTime? startDate, DateTime? endDate)
        {
            // Ensure the user is authenticated
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Retrieve properties owned by the host, including reservations
            var properties = await _context.Properties
                .Where(p => p.Host.Id == userId)
                .Include(p => p.Reservations)
                .ToListAsync();

            // Filter reservations based on the date range if provided
            if (startDate.HasValue && endDate.HasValue)
            {
                properties = properties
                    .Where(p => p.Reservations.Any(r => r.CheckIn <= endDate && r.CheckOut >= startDate))
                    .ToList();

            }

            // Generate the report data
            var reportData = properties.Select(p => new HostSummaryViewModel
            {
                PropertyName = p.PropertyName,
               
                TotalStayRevenue = p.Reservations
                    .Where(r => r.CheckIn <= endDate && r.CheckOut >= startDate)
                    .Sum(r => r.CalculateBaseTotal() *.90m), // Host gets 90% revenue
                TotalCleaningFees = p.Reservations
                    .Where(r => r.CheckIn <= endDate && r.CheckOut >= startDate)
                    .Sum(r => r.CleaningFee),
                CombinedRevenue = p.Reservations
                    .Where(r => r.CheckIn <= endDate && r.CheckOut >= startDate)
                    .Sum(r => (r.CalculateBaseTotal() *.90m) + r.CleaningFee),
                TotalReservations = p.Reservations
                    .Count(r => r.CheckIn <= endDate && r.CheckOut >= startDate)
            }).ToList();

            // Return the view with the generated data
            return View(reportData);
        }


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var roles = await _userManager.GetRolesAsync(user);

            IndexViewModel ivm = new IndexViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Birthday = user.Birthday,
                Role = roles.FirstOrDefault(),
                UserID = user.Id,
                HasPassword = true
            };

            return View(ivm);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel cpvm)
        {
            if (!ModelState.IsValid)
            {
                return View(cpvm);
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = await _userManager.ChangePasswordAsync(user, cpvm.OldPassword, cpvm.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(cpvm);
        }

        public IActionResult ChangeAccountDetails()
        {
            var user = _userManager.GetUserAsync(User).Result;

            var model = new ChangeAccountDetails
            {
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Birthday = user.Birthday
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccountDetails(ChangeAccountDetails model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            // Verify  pass
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.CurrentPassword, false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Incorrect password.");
                return View(model);
            }

            // Update user
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.Birthday = model.Birthday;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied(string ReturnURL)
        {
            return View("Error", new string[] { "Access is denied" });
        }
    }
}