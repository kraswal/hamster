using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using hamster.Data;
using hamsterModel;
using Microsoft.AspNetCore.Authorization;
using hamsterModel.ViewModels;
using Microsoft.AspNetCore.Identity;
using hamster.Models.Entities;

namespace hamster.Controllers
{
    [Authorize]
    public class ShareController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        public ShareController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Catalog()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

            MainViewModel mainViewModel = new MainViewModel()
            {
                Shares = Shares500.ReadShares(_db.Currencies),
                Portfolios = portfolios,
            };

            return View(mainViewModel);
        }
    }
}
