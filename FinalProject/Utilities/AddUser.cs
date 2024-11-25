using FinalProject.DAL;
using FinalProject.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FinalProject.Utilities
{
    public static class AddUser
    {
        public async static Task<IdentityResult> AddUserWithRoleAsync(AddUserModel aum, UserManager<AppUser> userManager, AppDbContext _context)
        {
            AppUser dbUser = await userManager.FindByEmailAsync(aum.User.Email);
            IdentityResult result;

            if (dbUser == null)
            {
                try
                {
                    result = await userManager.CreateAsync(aum.User, aum.Password);
                }
                catch (Exception ex)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.Append("Error adding the user with the email ");
                    msg.Append(aum.User.Email);
                    msg.Append(". This may happen if a required field is missing from AppUser.");
                    throw new Exception(msg.ToString(), ex);
                }

                if (!result.Succeeded)
                {
                    StringBuilder msg = new StringBuilder();
                    foreach (var error in result.Errors)
                    {
                        msg.AppendLine(error.Description);
                    }
                    throw new Exception("Failed to add user: " + msg.ToString());
                }

                dbUser = await userManager.FindByEmailAsync(aum.User.Email);
            }
            else
            {
                dbUser.PhoneNumber = aum.User.PhoneNumber;
                dbUser.FirstName = aum.User.FirstName;
                dbUser.LastName = aum.User.LastName;
                dbUser.Address = aum.User.Address;
                dbUser.Birthday = aum.User.Birthday;

                _context.Update(dbUser);
                _context.SaveChanges();

                var token = await userManager.GeneratePasswordResetTokenAsync(dbUser);
                result = await userManager.ResetPasswordAsync(dbUser, token, aum.Password);
            }

            if (!await userManager.IsInRoleAsync(dbUser, aum.RoleName))
            {
                await userManager.AddToRoleAsync(dbUser, aum.RoleName);
            }

            return result;
        }
    }

    public class AddUserModel
    {
        public AppUser User { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
    }
}