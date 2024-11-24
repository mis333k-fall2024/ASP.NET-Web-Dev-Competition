using fa22RelationalDataDemo.DAL;
using fa22RelationalDataDemo.Models;
using fa22RelationalDataDemo.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace fa22RelationalDataDemo.Controllers
{
    //only logged-in users can access registrations
    //kind of like orders in your HW4
    [Authorize]
    public class RegistrationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RegistrationsController(AppDbContext context, UserManager<AppUser> userManger)
        {
            _context = context;
            _userManager = userManger;
        }

        // GET: Registrations
        public IActionResult Index()
        {
            //Set up a list of registrations to display
            List<Registration> registrations;

            //User.IsInRole -- they see ALL registrations and detail
            if (User.IsInRole("Admin"))
            {
                registrations = _context.Registrations
                                .Include(r => r.RegistrationDetails)
                                .ToList();
            }
            else //user is a customer, so only display their records
            //registration is assocated with a particular user (look on the registration model class)
            //every logged in user is allowed to access index page, but their results will be different
            {
                registrations = _context.Registrations
                                .Include(r => r.RegistrationDetails)
                                .Where(r => r.User.UserName == User.Identity.Name)
                                .ToList();
            }

            //
            return View(registrations);
        }

        // GET: Registrations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //the user did not specify a registration to view
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a registration to view!" });
            }

            //find the registration in the database
            Registration registration = await _context.Registrations
                                              .Include(r => r.RegistrationDetails)
                                              .ThenInclude(r => r.Course)
                                              .Include(r => r.User)
                                              .FirstOrDefaultAsync(m => m.RegistrationID == id);

            //registration was not found in the database
            if (registration == null)
            {
                return View("Error", new String[] { "This registration was not found!" });
            }

            //make sure this registration belongs to this user
            if (User.IsInRole("Customer") && registration.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "This is not your order!  Don't be such a snoop!" });
            }

            //Send the user to the details page
            return View(registration);
        }

        //ONLY customers can create a registration -- business driven
        //make sure that the user selected a product; overrides controller-level authorization

        [Authorize(Roles = "Customer")]
        public IActionResult AddToCart(int? courseID)
        {
            if (courseID == null)
            {
                return View("Error", new string[] { "Please specify a course to add to the registration" });
            }

            //find the course in the database
            Course dbCourse = _context.Courses.Find(courseID);

            //make sure the product exists in the database
            if (dbCourse == null)
            {
                return View("Error", new string[] { "This course was not in the database!" });
            }

            //find the cart for this customer
            Registration reg = _context.Registrations.FirstOrDefault(r => r.User.UserName == User.Identity.Name && r.Status == RegistrationStatus.Pending);

            //if this registration is null, there isn't one yet, so create it
            if (reg == null)
            {
                //create a new object
                reg = new Registration();

                //update the generated properties of the registration
                reg.Status = RegistrationStatus.Pending;
                reg.RegistrationDate = DateTime.Now;
                reg.RegistrationNumber = GenerateNextRegistrationNumber.GetNextRegistrationNumber(_context);
                reg.User = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);

                //add the registration to the database
                _context.Registrations.Add(reg);
                _context.SaveChanges();
            }

            //now create the registration detail
            RegistrationDetail rd = new RegistrationDetail();

            //add the course to the registration detail
            rd.Course = dbCourse;

            //add the registration to the registration detail
            rd.Registration = reg;

            //you can assume the quantity is zero - they can edit it later
            rd.NumberOfStudents = 1;

            //calculate the properties on the regdetails
            rd.CourseFee = dbCourse.CourseFee;
            rd.TotalFees = dbCourse.CourseFee * rd.NumberOfStudents;

            //add the registration detail to the database
            _context.RegistrationDetails.Add(rd);
            _context.SaveChanges(true);

            //go to the details view
            return RedirectToAction("Details", new { id = reg.RegistrationID });
        }

        // GET: Registrations/Edit/5
        public IActionResult Edit(int? id)
        {
            //user did not specify a registration to edit
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a registration to edit" });
            }

            //find the registration in the database, and be sure to include details
            Registration registration = _context.Registrations
                                       .Include(r => r.RegistrationDetails)
                                       .ThenInclude(r => r.Course)
                                       .Include(r => r.User)
                                       .FirstOrDefault(r => r.RegistrationID == id);

            //registration was nout found in the database
            if (registration == null)
            {
                return View("Error", new String[] { "This registration was not found in the database!" });
            }

            //registration does not belong to this user
            if (User.IsInRole("Customer") && registration.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "You are not authorized to edit this registration!" });
            }

            //registration is complete - cannot be edited
            if (registration.Status == RegistrationStatus.Completed)
            {
                return View("Error", new string[] { "This order is complete and cannot be changed!" });
            }

            //send the user to the registration edit view
            return View(registration);
        }

        // POST: Registrations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Registration registration)
        {
            //this is a security measure to make sure the user is editing the correct registration
            if (id != registration.RegistrationID)
            {
                return View("Error", new String[] { "There was a problem editing this registration. Try again!" });
            }

            //if there is something wrong with this order, try again
            if (ModelState.IsValid == false)
            {
                return View(registration);
            }

            //if code gets this far, update the record
            try
            {
                //find the record in the database
                Registration dbRegistration = _context.Registrations.Find(registration.RegistrationID);

                //update the notes
                dbRegistration.RegistrationNotes = registration.RegistrationNotes;

                _context.Update(dbRegistration);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was an error updating this registration!", ex.Message });
            }

            //send the user to the Registrations Index page.
            return RedirectToAction(nameof(Index));
        }


        //GET: Registrations/Create
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Customer"))
            {
                Registration reg = new Registration();
                reg.User = await _userManager.FindByNameAsync(User.Identity.Name);
                return View(reg);
            }
            else
            {
                ViewBag.UserNames = await GetAllCustomerUserNamesSelectList();
                return View("SelectCustomerForRegistration");
            }
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        //create registration -- you don't get to tell what user you are -- we get it from the DB
        public async Task<IActionResult> Create([Bind("User, RegistrationNotes")] Registration registration)
        {
            //Find the next registration number from the utilities class
            registration.RegistrationNumber = Utilities.GenerateNextRegistrationNumber.GetNextRegistrationNumber(_context);

            //Set the date of this order
            registration.RegistrationDate = DateTime.Now;

            //Associate the registration with the logged-in customer
            registration.User = await _userManager.FindByNameAsync(registration.User.UserName);


            //if code gets this far, add the registration to the database
            _context.Add(registration);
            await _context.SaveChangesAsync();

            //send the user on to the action that will allow them to 
            //create a registration detail.  Be sure to pass along the RegistrationID
            //that you created when you added the registration to the database above
            return RedirectToAction("Create", "RegistrationDetails", new { registrationID = registration.RegistrationID });
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectCustomerForRegistration(String SelectedCustomer)
        {
            if (String.IsNullOrEmpty(SelectedCustomer))
            {
                ViewBag.UserNames = await GetAllCustomerUserNamesSelectList();
                return View("SelectCustomerForRegistration");
            }

            Registration reg = new Registration();
            reg.User = await _userManager.FindByNameAsync(SelectedCustomer);
            return View("Create", reg);
        }


        [Authorize]
        public async Task<IActionResult> CheckoutRegistration(int? id)
        {
            //the user did not specify a registration to view
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a registration to view!" });
            }

            //find the registration in the database
            Registration registration = await _context.Registrations
                                              .Include(r => r.RegistrationDetails)
                                              .ThenInclude(r => r.Course)
                                              .Include(r => r.User)
                                              .FirstOrDefaultAsync(m => m.RegistrationID == id);

            //registration was not found in the database
            if (registration == null)
            {
                return View("Error", new String[] { "This registration was not found!" });
            }

            //make sure this registration belongs to this user
            if (User.IsInRole("Customer") && registration.User.UserName != User.Identity.Name)
            {
                return View("Error", new String[] { "This is not your order!  Don't be such a snoop!" });
            }

            //Send the user to the details page
            return View("Confirm",registration);
        }

        public async Task<IActionResult> Confirm(int? id)
        {
            Registration dbReg = await _context.Registrations.FindAsync(id);
            dbReg.Status = RegistrationStatus.Completed;
            _context.Update(dbReg);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        public async Task<SelectList> GetAllCustomerUserNamesSelectList()
        {
            //create a list to hold the customers
            List<AppUser> allCustomers = new List<AppUser>();

            //See if the user is a customer
            foreach (AppUser dbUser in _context.Users)
            {
                //if the user is a customer, add them to the list of customers
                if (await _userManager.IsInRoleAsync(dbUser, "Customer") == true)//user is a customer
                { 
                    allCustomers.Add(dbUser);
                }
            }

            //create a new select list with the customer emails
            SelectList sl = new SelectList(allCustomers.OrderBy(c => c.Email), nameof(AppUser.UserName), nameof(AppUser.Email));

            //return the select list
            return sl;

        }
    }
}