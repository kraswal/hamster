using hamster.Data;
using hamster.Models.Entities;
using hamsterModel;
using hamsterModel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hamster.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        public SettingController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Portfolios()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            MainViewModel main = new MainViewModel()
            {
                Portfolios = portfolios,
            };

            return View(main);
        }

        public IActionResult toMainPage()
        {
            return RedirectToAction("Index", "Portfolio");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Portfolio port)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            var defportfolio = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            defportfolio.First().IsDefault = false;
            
            foreach(var p in portfolios)
            {
                if (p.PortfolioName == port.PortfolioName)
                {
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Create", port) });
                }
            }


            var portfolio = new Portfolio
            {
                PortfolioName = port.PortfolioName,
                UserId = user.Id,
                Commission = port.Commission * 0.01,
                Cost = port.Cost,
                DateOfCreation = DateTime.Now,
                IsDefault = true,
            };
            _db.Portfolios.AddRange(portfolio);

            _db.SaveChanges();

            var ps = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

            MainViewModel main = new MainViewModel()
            {
                Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
                Portfolio = ps.First(),
            };

            return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Update", main) });
        }

        [HttpGet]
        public IActionResult Update()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

            if (portfolios.Count() == 0)
            {
                return NotFound();
            }

            var portfolio = portfolios.First();

            if (portfolio == null)
            {
                return NotFound();
            }

            var mainportfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            MainViewModel main = new MainViewModel()
            {
                PortfolioName = portfolio.PortfolioName,
                Portfolios = mainportfolios,
                Portfolio = portfolio,
            };

            return View(main);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Portfolio portfolio)
        {            
            var oldPortfolio = _db.Portfolios.Find(portfolio.PortfolioId);

            oldPortfolio.PortfolioName = portfolio.PortfolioName;
            oldPortfolio.Commission = portfolio.Commission * 0.01;
            oldPortfolio.Cost = portfolio.Cost;
       
            _db.SaveChanges();
            return RedirectToAction("Update");
        }

        public IActionResult Choose()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            return View(portfolios);
        }

        public IActionResult Portfolio(int ?id)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.PortfolioId == id && p.UserId == user.Id select p;

            if (portfolios.Count() == 0)
            {
                return Content("Нет доступа.");
            }

            var portfolio = portfolios.First();
            portfolio.IsDefault = true;

            var portfolios2 = from p in _db.Portfolios where p.UserId == user.Id select p;
            foreach (var p in portfolios2)
            {
                if (p.PortfolioId != id)
                {
                    p.IsDefault = false;
                }
            }
            _db.SaveChanges();

            return RedirectToAction("Portfolio","Portfolio");
        }

        //[HttpGet]
        //public IActionResult Delete()
        //{
        //    var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

        //    var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

        //    return View(portfolios.First());
        //}

        //[HttpPost]
        //public IActionResult Delete(Portfolio port)
        //{
        //    var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

        //    var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

        //    _db.Portfolios.Remove(portfolios.First());

        //    _db.SaveChanges();

        //    var ps = from p in _db.Portfolios where p.UserId == user.Id select p;
        //    ps.First().IsDefault = true;

        //    _db.SaveChanges();

        //    MainViewModel main = new MainViewModel()
        //    {
        //        Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
        //        Portfolio = ps.First(),
        //    };

        //    return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Update", main) });
        //}
    }
}
