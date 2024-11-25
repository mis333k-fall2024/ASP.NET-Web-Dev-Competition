using FinalProject.Models;
using FinalProject.DAL;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinalProject.Utilities;

namespace FinalProject.Seeding
{
    public static class SeedUsers
    {
        public static async Task SeedAllUsers(UserManager<AppUser> userManager, AppDbContext context)
        {
            // Define the users you want to seed
            List<AddUserModel> AllUsers = new List<AddUserModel>
            {
                new AddUserModel
                {
                    User = new AppUser
                    {
                        UserName = "admin@example.com",
                        Email = "admin@example.com",
                        PhoneNumber = "5125551234",
                        FirstName = "Admin",
                        LastName = "User",
                        Address = "123 Main St"
                    },
                    Password = "Admin123!", 
                    RoleName = "Admin" 
                },
                new AddUserModel
                {
                    User = new AppUser
                    {
                        UserName = "customer@example.com",
                        Email = "customer@example.com",
                        PhoneNumber = "5125555678",
                        FirstName = "Customer",
                        LastName = "User",
                        Address = "123 Main St",
                    },
                    Password = "Customer123!", 
                    RoleName = "Customer" 
                }
            };

            // Seed each user
            foreach (var addUser in AllUsers)
            {
                // Check if the user already exists
                AppUser existingUser = await userManager.FindByEmailAsync(addUser.User.Email);
                if (existingUser == null)
                {
                    // Create the user
                    IdentityResult result = await userManager.CreateAsync(addUser.User, addUser.Password);
                    if (result.Succeeded)
                    {
                        // Assign the role to the user
                        await userManager.AddToRoleAsync(addUser.User, addUser.RoleName);
                    }
                    else
                    {
                        // Handle errors during user creation
                        throw new Exception($"Error creating user {addUser.User.Email}: {string.Join(", ", result.Errors)}");
                    }
                }
            }
        }
    }
}
