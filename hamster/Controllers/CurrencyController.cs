using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamster.Data;
using hamster.Models.Entities;
using hamsterModel;
using hamsterModel.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace hamster.Controllers
{
    [Authorize(Policy = "Administrator")]
    public class CurrencyController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CurrencyController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            List<Currency> currencies = new List<Currency>(_db.Currencies.OrderBy(c => c.CurrencyName));
            return View(currencies);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Currency currency)
        {
            _db.Currencies.Add(currency);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
