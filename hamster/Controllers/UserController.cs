using hamster.Data;
using hamster.Models;
using hamster.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterModel;
using System;

namespace hamster.Controllers
{
    public class UserController : Controller
    {

        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _db;
        public AppUser _appUser;

        public UserController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, AppDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь не найден");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                if (!user.IsAdmin)
                    return Redirect("/Portfolio");
                else
                    return Redirect("/Admin");
            }
            else
            {
                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(model);
            }
        }

        public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.UserName,
                Nickname = model.Nickname,
                Password = model.Password,
            };

            var result = await _userManager.CreateAsync(user,model.Password);

            if (result.Succeeded)
            {
                var user2 = new AppUser
                {
                    UserName = model.UserName,
                    Nickname = model.Nickname,
                    Password = model.Password,
                };
                _db.Users.AddRange(user2);
                _db.SaveChanges();

                // создаем новому пользователю портфель по-умолчанию

                var portfolio = new Portfolio
                {
                    PortfolioName = "По-умолчанию",
                    UserId = user.Id,
                    Commission = 0,
                    Cost = 0,
                    DateOfCreation = DateTime.Now,
                    IsDefault = true,
                };
                _db.Portfolios.AddRange(portfolio);

                // создаем новому пользователю рублевый счет

                //var currencyAccount = new CurrencyAccount
                //{
                //    C
                //}

                _db.SaveChanges();

                await _signInManager.SignInAsync(user, false);
                return Redirect("/Portfolio/Index");
            }
            else
            {
                ModelState.AddModelError("", "Имя пользователя занято");
                return View(model);
            }
        }

        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/Home/Index");
        }
    }
}