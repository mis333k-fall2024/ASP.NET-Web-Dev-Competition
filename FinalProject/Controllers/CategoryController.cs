using fa22RelationalDataDemo.DAL;
using fa22RelationalDataDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace fa22RelationalDataDemo.Controllers
{
    //HAVE to be an admin to access the entire controller and create/manage departments
    //you'll get an error is you're not in that role
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            return View(await _context.Departments.ToListAsync());
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a department to view!" });
            }

            //find the department in the database
            Department department = await _context.Departments
                .Include(d => d.Courses)
                .FirstOrDefaultAsync(m => m.DepartmentID == id);

            //if the department is not in the database, show the user an error
            if (department == null)
            {
                return View("Error", new String[] { "This department was not found!" });
            }

            //show the user the details for this department
            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //user did not specify a department to edit            
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a department to edit!" });
            }

            //find the department in the database
            Department department = await _context.Departments.FindAsync(id);

            //see if the department exists in the database
            if (department == null)
            {
                return View("Error", new String[] { "This department does not exist in the database!" });
            }

            //send the user to the edit department page
            return View(department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            //this is a security measure to make sure they are editing the correct department
            if (id != department.DepartmentID)
            {
                return View("Error", new String[] { "There was a problem editing this department. Try again!" });
            }

            //if the user messed up, send them back to the view to try again
            if (ModelState.IsValid == false)
            {
                return View(department);
            }

            //if code gets this far, make the updates
            try
            {
                _context.Update(department);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was a problem editing this department.", ex.Message });
            }

            //send the user back to the view with all the departments
            return RedirectToAction(nameof(Index));
        }
    }
}
