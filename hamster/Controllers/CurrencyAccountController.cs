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

namespace hamster.Controllers
{
    public class CurrencyAccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CurrencyAccountController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId select c;


            List<CurrencyPair> currencyPairs = Shares500.ReadCurrencyPairs();
            List<CurrencyAccountForView> currencyAccountForViews = new List<CurrencyAccountForView>();



            foreach (var currencyAccount in currencyAccounts)
            {
                CurrencyAccountForView currency = new CurrencyAccountForView()
                {
                    CurrencyName = currencyAccount.CurrencyName,
                    CurrencyTicker = currencyAccount.CurrencyTicker,
                    CurrencySign = currencyAccount.CurrencySign,
                    Amount = currencyAccount.Amount,
                };
                currencyAccountForViews.Add(currency);
            }
            foreach (var currency in currencyAccountForViews)
            {
                if (currency.CurrencyTicker != "RUB")
                {
                    foreach (var currencyPair in currencyPairs)
                    {
                        if (currency.CurrencyTicker == currencyPair.BuyCurrency && "RUB" == currencyPair.PayCurrency)
                        {
                            currency.Value += currency.Amount * currencyPair.Rate;
                        }
                    }
                }
                else
                {
                    currency.Value += currency.Amount;
                }
            }

            MainViewModel mainViewModel = new MainViewModel()
            {
                Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
                CurrencyAccountForViews = currencyAccountForViews,
            };

            return View(mainViewModel);
        }
        [HttpGet]
        public IActionResult Deposit(int id)
        {
            CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);
            return View(currencyAccountModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deposit(CurrencyAccount currencyAccount)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            var currencyAccounts = from c in _db.CurrencyAccounts where c.CurrencyName == currencyAccount.CurrencyName && c.PortfolioId == portfolio.PortfolioId select c;
            if (currencyAccounts.Count() == 0)
            {
                foreach (var currency in _db.Currencies)
                {
                    if (currencyAccount.CurrencyName == currency.CurrencyName)
                    {
                        currencyAccount.CurrencyId = currency.CurrencyId;
                        currencyAccount.CurrencySign = currency.CurrencySign;
                        currencyAccount.CurrencyTicker = currency.CurrencyTicker;
                        currencyAccount.PortfolioId = portfolio.PortfolioId;
                    }
                }
                _db.CurrencyAccounts.Add(currencyAccount);
                _db.Transactions.AddRange(new Transaction
                {
                    AssetName = currencyAccount.CurrencyName,
                    AssetTicker = currencyAccount.CurrencyTicker,
                    TransactionDate = DateTime.Now,
                    TransactionAmount = Convert.ToInt32(currencyAccount.Amount),
                    TransactionCost = Convert.ToInt32(currencyAccount.Amount),
                    TransactionType = TransactionType.Пополнение,
                    CurrencyTicker = currencyAccount.CurrencyTicker,
                    CurrencyName = currencyAccount.CurrencyName,
                    CurrencySign = currencyAccount.CurrencySign,
                    PortfolioId = portfolio.PortfolioId,
                });
            }
            else
            {
                _db.Transactions.AddRange(new Transaction 
                {
                    AssetName = currencyAccount.CurrencyName,
                    AssetTicker = currencyAccounts.First().CurrencyTicker,
                    TransactionDate = DateTime.Now,
                    TransactionAmount = Convert.ToInt32(currencyAccount.Amount),
                    TransactionCost = currencyAccount.Amount,
                    TransactionType = TransactionType.Пополнение,
                    CurrencyTicker = currencyAccount.CurrencyTicker,
                    CurrencyName = currencyAccounts.First().CurrencyName,
                    CurrencySign = currencyAccounts.First().CurrencySign,
                    PortfolioId = portfolio.PortfolioId,
                });
                currencyAccounts.First().Amount += currencyAccount.Amount;
            }
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Withdraw(int id)
        {
            CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);
            return View(currencyAccountModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Withdraw(CurrencyAccount currencyAccount)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            var currencyAccounts = from c in _db.CurrencyAccounts where (c.CurrencyName == currencyAccount.CurrencyName && c.PortfolioId == portfolio.PortfolioId) select c;
            if (currencyAccounts.Count() == 0)
            {
                ModelState.AddModelError("", "У вас нет средств в данной валюте.");
                CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);
                return View(currencyAccountModel);
            }

            if (currencyAccounts.First().Amount < currencyAccount.Amount)
            {
                ModelState.AddModelError("", "Недостаточно средств.");
                CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);
                return View(currencyAccountModel);
            } 

            else
            {
                if (currencyAccounts.First().Amount == currencyAccount.Amount)
                {
                    _db.Transactions.AddRange(new Transaction
                    {
                        AssetName = currencyAccount.CurrencyName,
                        AssetTicker = currencyAccounts.First().CurrencyTicker,
                        TransactionDate = DateTime.Now,
                        TransactionAmount = Convert.ToInt32(currencyAccount.Amount),
                        TransactionCost = Convert.ToInt32(currencyAccount.Amount),
                        TransactionType = TransactionType.Вывод,
                        CurrencyTicker = currencyAccount.CurrencyTicker,
                        CurrencyName = currencyAccounts.First().CurrencyName,
                        CurrencySign = currencyAccounts.First().CurrencySign,
                        PortfolioId = portfolio.PortfolioId,
                    });
                    _db.CurrencyAccounts.Remove(currencyAccounts.First());
                }
                else
                {
                    _db.Transactions.AddRange(new Transaction
                    {
                        AssetName = currencyAccount.CurrencyName,
                        AssetTicker = currencyAccounts.First().CurrencyTicker,
                        TransactionDate = DateTime.Now,
                        TransactionAmount = Convert.ToInt32(currencyAccount.Amount),
                        TransactionCost = Convert.ToInt32(currencyAccount.Amount),
                        TransactionType = TransactionType.Вывод,
                        CurrencyTicker = currencyAccount.CurrencyTicker,
                        CurrencyName = currencyAccounts.First().CurrencyName,
                        CurrencySign = currencyAccounts.First().CurrencySign,
                        PortfolioId = portfolio.PortfolioId,
                    });
                    currencyAccounts.First().Amount -= currencyAccount.Amount;
                }
            }
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
