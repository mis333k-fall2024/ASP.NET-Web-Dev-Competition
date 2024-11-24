using fa22RelationalDataDemo.DAL;
using fa22RelationalDataDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fa22RelationalDataDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses
                .Include(c => c.Departments)
                .ToListAsync());
        }

        // GET: Courses/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            //id was not specified - show the user an error
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a course to view!" });
            }

            //find the course in the database
            //be sure to include the relevant navigational data
            Course course = await _context.Courses
                .Include(c => c.Departments)
                .FirstOrDefaultAsync(m => m.CourseID == id);

            //course was not found in the database
            if (course == null)
            {
                return View("Error", new String[] { "That course was not found in the database." });
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewBag.AllDepartments = GetDepartmentSelectList();
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course, int[] SelectedDepartments)
        {
            //This code has been modified so that if the model state is not valid
            //we immediately go to the "sad path" and give the user a chance to try again
            if (ModelState.IsValid == false)
            {
                //re-populate the view bag with the departments
                ViewBag.AllDepartments = GetDepartmentSelectList();
                //go back to the Create view to try again
                return View(course);
            }

            //if code gets to this point, we know the model is valid and
            //we can add the course to the database

            //add the course to the database and save changes
            _context.Add(course);
            await _context.SaveChangesAsync();

            //add the associated departments to the course
            //loop through the list of deparment ids selected by the user
            foreach (int departmentID in SelectedDepartments)
            {
                //find the department associated with that id
                Department dbDepartment = _context.Departments.Find(departmentID);

                //add the department to the course's list of departments and save changes
                course.Departments.Add(dbDepartment);
                _context.SaveChanges();
            }

            //Send the user to the page with all the departments
            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //if the user didn't specify a course id, we can't show them 
            //the data, so show an error instead
            if (id == null)
            {
                return View("Error", new string[] { "Please specify a course to edit!" });
            }

            //find the course in the database
            //be sure to change the data type to course instead of 'var'
            Course course = await _context.Courses.Include(c => c.Departments)
                                           .FirstOrDefaultAsync(c => c.CourseID == id);

            //if the course does not exist in the database, then show the user
            //an error message
            if (course == null)
            {
                return View("Error", new string[] { "This course was not found!" });
            }

            //populate the viewbag with existing departments
            ViewBag.AllDepartments = GetDepartmentSelectList(course);
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Course course, int[] SelectedDepartments)
        {
            //this is a security check to see if the user is trying to modify
            //a different record.  Show an error message
            if (id != course.CourseID)
            {
                return View("Error", new string[] { "Please try again!" });
            }

            if (ModelState.IsValid == false) //there is something wrong
            {
                ViewBag.AllDepartments = GetDepartmentSelectList(course);
                return View(course);
            }

            //if code gets this far, attempt to edit the course
            try
            {
                //Find the course to edit in the database and include relevant 
                //navigational properties
                Course dbCourse = _context.Courses
                    .Include(c => c.Departments)
                    .FirstOrDefault(c => c.CourseID == course.CourseID);

                //create a list of departments that need to be removed
                List<Department> DepartmentsToRemove = new List<Department>();

                //find the departments that should no longer be selected because the
                //user removed them
                //remember, SelectedDepartments = the list from the HTTP request (listbox)
                foreach (Department department in dbCourse.Departments)
                {
                    //see if the new list contains the department id from the old list
                    if (SelectedDepartments.Contains(department.DepartmentID) == false)//this department is not on the new list
                    {
                        DepartmentsToRemove.Add(department);
                    }
                }

                //remove the departments you found in the list above
                //this has to be 2 separate steps because you can't iterate (loop)
                //over a list that you are removing things from
                foreach (Department department in DepartmentsToRemove)
                {
                    //remove this course department from the course's list of departments
                    dbCourse.Departments.Remove(department);
                    _context.SaveChanges();
                }

                //add the departments that aren't already there
                foreach (int departmentID in SelectedDepartments)
                {
                    if (dbCourse.Departments.Any(d => d.DepartmentID == departmentID) == false)//this department is NOT already associated with this course
                    {
                        //Find the associated department in the database
                        Department dbDepartment = _context.Departments.Find(departmentID);

                        //Add the department to the course's list of departments
                        dbCourse.Departments.Add(dbDepartment);
                        _context.SaveChanges();
                    }
                }

                //update the course's scalar properties
                dbCourse.CourseFee = course.CourseFee;
                dbCourse.CourseName = course.CourseName;
                dbCourse.Description = course.Description;

                //save the changes
                _context.Courses.Update(dbCourse);
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                return View("Error", new string[] { "There was an error editing this course.", ex.Message });
            }

            //if code gets this far, everything is okay
            //send the user back to the page with all the courses
            return RedirectToAction(nameof(Index));
        }
        private MultiSelectList GetDepartmentSelectList()
        {
            //Create a new list of departments and get the list of the departments
            //from the database
            List<Department> allDepartments = _context.Departments.ToList();

            //Multi-select lists do not require a selection, so you don't need 
            //to add a dummy record like you do for select lists

            //use the MultiSelectList constructor method to get a new MultiSelectList
            MultiSelectList mslAllDepartments = new MultiSelectList(allDepartments.OrderBy(d => d.DepartmentName), "DepartmentID", "DepartmentName");

            //return the MultiSelectList
            return mslAllDepartments;
        }

        private MultiSelectList GetDepartmentSelectList(Course course)
        {
            //Create a new list of departments and get the list of the departments
            //from the database
            List<Department> allDepartments = _context.Departments.ToList();

            //loop through the list of course departments to find a list of department ids
            //create a list to store the department ids
            List<Int32> selectedDepartmentIDs = new List<Int32>();

            //Loop through the list to find the DepartmentIDs
            foreach (Department associatedDepartment in course.Departments)
            {
                selectedDepartmentIDs.Add(associatedDepartment.DepartmentID);
            }

            //use the MultiSelectList constructor method to get a new MultiSelectList
            MultiSelectList mslAllDepartments = new MultiSelectList(allDepartments.OrderBy(d => d.DepartmentName), "DepartmentID", "DepartmentName", selectedDepartmentIDs);

            //return the MultiSelectList
            return mslAllDepartments;
        }
    }
}
