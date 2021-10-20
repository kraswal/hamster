using hamster.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System;

namespace hamster.Data
{
    public static class DatabaseInitializer
    {
        public static void Init(IServiceProvider serviceProvider, AppDbContext db)
        {
            var userManager = serviceProvider.GetService<UserManager<AppUser>>();

            foreach (var appUser in db.Users)
            {
                var user = new AppUser
                {
                    Id = appUser.Id,
                    UserName = appUser.UserName,
                    Nickname = appUser.Nickname,
                    IsAdmin = appUser.IsAdmin,
                    Password = appUser.Password,
                };

                var result = userManager.CreateAsync(user, user.Password).GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    if (user.IsAdmin == false)
                    {
                        userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "User")).GetAwaiter().GetResult();
                    }
                    else
                    {
                        userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Administrator")).GetAwaiter().GetResult();
                    }
                }
            }
        }
    }
}
