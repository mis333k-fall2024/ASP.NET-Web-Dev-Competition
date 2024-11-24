using fa22RelationalDataDemo.DAL;
using fa22RelationalDataDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fa22RelationalDataDemo.Controllers
{
    public class RegistrationDetailsController : Controller
    {
        private readonly AppDbContext _context;

        public RegistrationDetailsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: RegistrationDetails
        public IActionResult Index(int? registrationID)
        {
            if (registrationID == null)
            {
                return View("Error", new String[] { "Please specify a registration to view!" });
            }

            //limit the list to only the registration details that belong to this registration
            List<RegistrationDetail> rds = _context.RegistrationDetails
                                          .Include(rd => rd.Course)
                                          .Where(rd => rd.Registration.RegistrationID == registrationID)
                                          .ToList();

            return View(rds);
        }

        // GET: RegistrationDetails/Create
        public IActionResult Create(int registrationID)
        {
            //create a new instance of the RegistrationDetail class
            RegistrationDetail rd = new RegistrationDetail();

            //find the registration that should be associated with this registration
            Registration dbRegistration = _context.Registrations.Find(registrationID);

            //set the new registration detail's registration equal to the registration you just found
            rd.Registration = dbRegistration;

            //populate the ViewBag with a list of existing courses
            ViewBag.AllCourses = GetCourseSelectList();

            //pass the newly created registration detail to the view
            return View(rd);
        }

        // POST: RegistrationDetails/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrationDetail registrationDetail, int SelectedCourse)
        {
            //if user has not entered all fields, send them back to try again
            if (ModelState.IsValid == false)
            {
                ViewBag.AllCourses = GetCourseSelectList();
                return View(registrationDetail);
            }

            //find the course to be associated with this order
            Course dbCourse = _context.Courses.Find(SelectedCourse);

            //set the registration detail's course to be equal to the one we just found
            registrationDetail.Course = dbCourse;

            //find the registration on the database that has the correct registration id
            //unfortunately, the HTTP request will not contain the entire registration object, 
            //just the registration id, so we have to find the actual object in the database
            Registration dbRegistration = _context.Registrations.Find(registrationDetail.Registration.RegistrationID);

            //set the registration on the registration detail equal to the registration that we just found
            registrationDetail.Registration = dbRegistration;

            //set the registration detail's price equal to the course price
            //this will allow us to to store the price that the user paid
            registrationDetail.CourseFee = dbCourse.CourseFee;

            //calculate the extended price for the registration detail
            registrationDetail.TotalFees = registrationDetail.NumberOfStudents * registrationDetail.CourseFee;

            //add the registration detail to the database
            _context.Add(registrationDetail);
            await _context.SaveChangesAsync();

            //Send the email to confirm order details have been added
            try
            {
                String emailBody = "Hello!\n\nThank you for your registration\n\n Course: " + dbCourse.CourseName + "\n\nTotal Cost: $" + registrationDetail.TotalFees;
                Utilities.EmailMessaging.SendEmail("Longhorn Code Academy - Registration Created", emailBody);
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was a problem sending the email", ex.Message });
            }

            //send the user to the details page for this registration
            return RedirectToAction("Details", "Registrations", new { id = registrationDetail.Registration.RegistrationID });
        }

        // GET: RegistrationDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //user did not specify a registration detail to edit
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a registration detail to edit!" });
            }

            //find the registration detail
            RegistrationDetail registrationDetail = await _context.RegistrationDetails
                                                   .Include(rd => rd.Course)
                                                   .Include(rd => rd.Registration)
                                                   .FirstOrDefaultAsync(rd => rd.RegistrationDetailID == id);
            if (registrationDetail == null)
            {
                return View("Error", new String[] { "This registration detail was not found" });
            }
            return View(registrationDetail);
        }

        // POST: RegistrationDetails/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegistrationDetail registrationDetail)
        {
            //this is a security check to make sure they are editing the correct record
            if (id != registrationDetail.RegistrationDetailID)
            {
                return View("Error", new String[] { "There was a problem editing this record. Try again!" });
            }

            //create a new registration detail
            RegistrationDetail dbRD;
            //if code gets this far, update the record
            try
            {
                //find the existing registration detail in the database
                //include both registration and course
                dbRD = _context.RegistrationDetails
                      .Include(rd => rd.Course)
                      .Include(rd => rd.Registration)
                      .FirstOrDefault(rd => rd.RegistrationDetailID == registrationDetail.RegistrationDetailID);

                //information is not valid, try again
                if (ModelState.IsValid == false)
                {
                    return View(registrationDetail);
                }

                //update the scalar properties
                dbRD.NumberOfStudents = registrationDetail.NumberOfStudents;
                dbRD.CourseFee = dbRD.Course.CourseFee;
                dbRD.TotalFees = dbRD.NumberOfStudents * dbRD.CourseFee;

                //save changes
                _context.Update(dbRD);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was a problem editing this record", ex.Message });
            }

            //Send the email to confirm order has been changed
            try
            {
                Utilities.EmailMessaging.SendEmail("Longhorn Academy - Registration Updated", "Cool story bro");
            }
            catch (Exception ex)
            {
                return View("Error", new String[] { "There was a problem sending the email", ex.Message });
            }

            //if code gets this far, go back to the registration details index page
            return RedirectToAction("Details", "Registrations", new { id = dbRD.Registration.RegistrationID });
        }

        // GET: RegistrationDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //user did not specify a registration detail to delete
            if (id == null)
            {
                return View("Error", new String[] { "Please specify a registration detail to delete!" });
            }

            //find the registration detail in the database
            RegistrationDetail registrationDetail = await _context.RegistrationDetails
                                                    .Include(r => r.Registration)
                                                   .FirstOrDefaultAsync(m => m.RegistrationDetailID == id);

            //registration detail was not found in the database
            if (registrationDetail == null)
            {
                return View("Error", new String[] { "This registration detail was not in the database!" });
            }

            //send the user to the delete confirmation page
            return View(registrationDetail);
        }

        // POST: RegistrationDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //find the registration detail to delete
            RegistrationDetail registrationDetail = await _context.RegistrationDetails
                                                   .Include(r => r.Registration)
                                                   .FirstOrDefaultAsync(r => r.RegistrationDetailID == id);

            //delete the registration detail
            _context.RegistrationDetails.Remove(registrationDetail);
            await _context.SaveChangesAsync();

            //return the user to the registration/details page
            return RedirectToAction("Details", "Registrations", new { id = registrationDetail.Registration.RegistrationID });
        }

        private SelectList GetCourseSelectList()
        {
            //create a list for all the courses
            List<Course> allCourses = _context.Courses.ToList();

            //the user MUST select a course, so you don't need a dummy option for no course

            //use the constructor on select list to create a new select list with the options
            SelectList slAllCourses = new SelectList(allCourses, nameof(Course.CourseID), nameof(Course.CourseName));

            return slAllCourses;
        }
    }
}