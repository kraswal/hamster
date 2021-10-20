using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamster.Data;
using hamster.Models.Entities;
using hamsterModel;
using hamsterModel.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace hamster.Controllers
{
    [Authorize]
    public class CurrencyPairController : Controller
    {
        private readonly AppDbContext _db;
        public CurrencyPairController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
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
    }
}
