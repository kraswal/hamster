using hamster.Data;
using hamster.Models.Entities;
using hamsterModel;
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
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public AdminController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Currency()
        {
            List<Currency> currencies = new List<Currency>(_db.Currencies.OrderBy(c => c.CurrencyName));
            return View(currencies);
        }
        [HttpGet]
        public IActionResult CurrencyCreate()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CurrencyCreate(Currency currency)
        {
            _db.Currencies.Add(currency);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult CurrencyPair()
        {
            List<CurrencyPair> currencyPairs = Shares500.ReadCurrencyPairs();
            foreach (var currencyPair in currencyPairs)
            {
                foreach (var currency in _db.Currencies)
                {
                    if (currencyPair.BuyCurrency == currency.CurrencyTicker)
                    {
                        currencyPair.BuyCurrencyName = currency.CurrencyName;
                        currencyPair.BuyCurrencySign = currency.CurrencySign;
                    }
                    if (currencyPair.PayCurrency == currency.CurrencyTicker)
                    {
                        currencyPair.PayCurrencyName = currency.CurrencyName;
                        currencyPair.PayCurrencySign = currency.CurrencySign;
                    }
                }
            }
            return View(currencyPairs);
        }

        public IActionResult Catalog()
        {
            IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);
            return View(shares);
        }

        public IActionResult Tariffs()
        {
            List<Tariff> tariffs = new List<Tariff>(_db.Tariffs.OrderBy(t => t.BrokerId));
            return View(tariffs);
        }

        public IActionResult Brokers()
        {
            List<Broker> brokers = new List<Broker>(_db.Brokers.OrderBy(b => b.BrokerName));
            return View(brokers);
        }

    }
}
