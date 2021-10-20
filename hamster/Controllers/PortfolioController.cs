using hamster.Data;
using hamster.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamsterModel;
using hamsterModel.ViewModels;
using hamsterModel.Logic;

namespace hamster.Controllers
{
    [Authorize]
    public class PortfolioController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public PortfolioController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Change(int id)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            foreach(var portfolio in portfolios)
            {
                if (portfolio.IsDefault == true) portfolio.IsDefault = false;
            }
            foreach (var portfolio in portfolios)
            {
                if (portfolio.PortfolioId == id) portfolio.IsDefault = true;
            }

            _db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Index()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            Portfolio portfolio = portfolios.First();

            var sharesAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;
            var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId select c;
            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;

            PortfolioViewModel portfolioViewModel = new PortfolioViewModel(portfolio.PortfolioId, sharesAccounts, currencyAccounts, transactions);
            portfolioViewModel.PortfolioName = portfolio.PortfolioName;
            portfolioViewModel.Commission = portfolio.Commission;

            // читаем курсы валютных пар
            List<CurrencyPair> currencyPairs = Shares500.ReadCurrencyPairs();

            foreach (var shareAccount in sharesAccounts)
            {
                foreach (var share in portfolioViewModel.Shares)
                {
                    if (shareAccount.ShareTicker == share.Ticker && shareAccount.PortfolioId == portfolio.PortfolioId)
                    {
                        foreach (var currencyPair in currencyPairs)
                        {
                            if (shareAccount.CurrencyTicker != "RUB")
                            {
                                if (shareAccount.CurrencyTicker == currencyPair.BuyCurrency && "RUB" == currencyPair.PayCurrency)
                                {
                                    portfolioViewModel.Value += shareAccount.Amount * share.Price * currencyPair.Rate;
                                    portfolioViewModel.ShareValue += shareAccount.Amount * share.Price * currencyPair.Rate;
                                    portfolioViewModel.Profit += (share.Price * shareAccount.Amount - shareAccount.AveragePrice * shareAccount.Amount) * currencyPair.Rate;
                                }
                            }
                            else
                            {
                                portfolioViewModel.ShareValue += shareAccount.Amount * share.Price;
                                portfolioViewModel.Value += shareAccount.Amount * share.Price;
                            }
                        }
                    }
                }
            }

            foreach (var currencyAccount in currencyAccounts)
            {
                if (currencyAccount.CurrencyTicker != "RUB")
                {
                    foreach (var currencyPair in currencyPairs)
                    {
                        if (currencyAccount.CurrencyTicker == currencyPair.BuyCurrency && "RUB" == currencyPair.PayCurrency)
                        {
                            portfolioViewModel.Value += currencyAccount.Amount * currencyPair.Rate;
                        }
                    }
                }
                else
                {
                    portfolioViewModel.Value += currencyAccount.Amount;
                }
            }

            var mainportfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            //MainViewModel main = new MainViewModel(portfolioViewModel,mainportfolios,sharesAccounts,currencyAccounts,transactions);

            MainViewModel main = new MainViewModel()
            {
                PortfolioName = portfolioViewModel.PortfolioName,
                Value = portfolioViewModel.Value,
                ShareValue = portfolioViewModel.ShareValue,
                Profit = portfolioViewModel.Profit,
                Portfolios = mainportfolios,
                ShareAccounts = sharesAccounts,
                CurrencyAccounts = currencyAccounts,
                Transactions = transactions,
            };


            return View(main);
        }

        [HttpGet]
        [NoDirectAccess]
        public IActionResult BuyOrSell()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            ViewBag.DefaultCommission = portfolio.Commission;

            IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);
            ShareAccountModel shareAccountModel = new ShareAccountModel(shares);

            return PartialView(shareAccountModel);
        }

        [HttpPost]
        public IActionResult BuyOrSell(Transaction transaction)
        {
            if (transaction.TransactionType == TransactionType.Покупка)
            {
                var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

                var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
                var portfolio = portfolios.First();

                var shareAccounts = from s in _db.ShareAccounts where s.ShareName == transaction.AssetName && s.PortfolioId == portfolio.PortfolioId select s;

                IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);

                transaction.TransactionComission = transaction.TransactionComission * Convert.ToDecimal(0.01);
                transaction = TransactionLogic.CompleteTransaction(transaction, _db.Currencies, TransactionType.Покупка, portfolio.PortfolioId);

                if (transaction.Internal == true)
                {
                    var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId && c.CurrencyTicker == transaction.CurrencyTicker select c;

                    if (currencyAccounts.Count() == 0)
                    {
                        ModelState.AddModelError("", "Недостаточно средств.");
                        ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                        shareAccountModel.Transaction = transaction;
                        ViewBag.DefaultCommission = portfolio.Commission;
                        return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "BuyOrSell", shareAccountModel) });
                    }

                    var currencyAccount = currencyAccounts.First();

                    if (currencyAccount.Amount < transaction.TransactionCost)
                    {
                        ModelState.AddModelError("", "Недостаточно средств.");
                        ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                        shareAccountModel.Transaction = transaction;
                        ViewBag.DefaultCommission = portfolio.Commission;
                        return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "BuyOrSell", shareAccountModel) });
                    }
                    else
                    {
                        currencyAccount.Amount -= transaction.TransactionCost + transaction.TransactionComission;
                    }
                }

                _db.Transactions.Add(transaction);

                if (shareAccounts.Count() == 0)
                {
                    _db.ShareAccounts.AddRange(ShareAccountLogic.BuyNewShare(transaction));
                }
                else
                {
                    ShareAccountLogic.BuyExistingShare(shareAccounts.First(), transaction);
                }

                _db.SaveChanges();

                MainViewModel main = new MainViewModel()
                {
                    ShareAccounts = shareAccounts,
                    Shares = shares,
                    Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
                };

                return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", main) });
            }

            // ПРОДАЖА

            else
            {
                var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

                var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
                var portfolio = portfolios.First();

                IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);

                var shareAccounts = from s in _db.ShareAccounts where s.ShareName == transaction.AssetName && s.PortfolioId == portfolio.PortfolioId select s;

                // проверка на наличие таких акций и их количество

                if (shareAccounts.Count() == 0)
                {
                    ModelState.AddModelError("", "В портфеле нет акций " + transaction.AssetName);
                    ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                    ViewBag.DefaultCommission = portfolio.Commission;
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "BuyOrSell", shareAccountModel) });
                }

                var shareAccount = shareAccounts.First();

                if (shareAccount.Amount < transaction.TransactionAmount)
                {
                    ModelState.AddModelError("", "В портфеле недостаточно акций " + transaction.AssetName);
                    ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                    ViewBag.DefaultCommission = portfolio.Commission;
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "BuyOrSell", shareAccountModel) });
                }
                // 

                // создаем или находим валютный счет, чтобы пополнить его средствами с продажи

                var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId && transaction.CurrencyTicker == c.CurrencyTicker select c;


                foreach (var share in shares)
                {
                    if (share.ShareName == transaction.AssetName) transaction.CurrencyTicker = share.CurrencyTicker;
                }

                var currencies = from c in _db.Currencies where c.CurrencyTicker == transaction.CurrencyTicker select c;
                var currency = currencies.First();


                var currencyAccount = new CurrencyAccount();

                if (currencyAccounts.Count() == 0)
                {
                    currencyAccount.CurrencyId = currency.CurrencyId;
                    currencyAccount.CurrencyTicker = currency.CurrencyTicker;
                    currencyAccount.CurrencyName = currency.CurrencyName;
                    currencyAccount.CurrencySign = currency.CurrencySign;
                    currencyAccount.Amount = 0;
                    currencyAccount.PortfolioId = portfolio.PortfolioId;

                    _db.CurrencyAccounts.Add(currencyAccount);
                }
                else
                {
                    currencyAccount = currencyAccounts.First();
                }

                // продается актив, создается транзакция и пополняется счет

                ShareAccount shareAcc = shareAccounts.First();

                transaction.TransactionComission = transaction.TransactionComission * Convert.ToDecimal(0.01);

                if (shareAcc.Amount == transaction.TransactionAmount)
                {
                    transaction = TransactionLogic.CompleteTransaction(transaction, _db.Currencies, TransactionType.Продажа, portfolio.PortfolioId);
                    transaction.TradeResult = (transaction.TransactionAmount * transaction.TransactionPrice) - (transaction.TransactionAmount * shareAcc.AveragePrice);
                    currencyAccount.Amount += transaction.TransactionCost - transaction.TransactionComission;
                    _db.Transactions.Add(transaction);
                    _db.ShareAccounts.Remove(shareAcc);
                }
                else
                {
                    transaction = TransactionLogic.CompleteTransaction(transaction, _db.Currencies, TransactionType.Продажа, portfolio.PortfolioId);
                    transaction.TradeResult = (transaction.TransactionAmount * transaction.TransactionPrice) - (transaction.TransactionAmount * shareAcc.AveragePrice);
                    _db.Transactions.Add(transaction);
                    shareAcc.Amount -= transaction.TransactionAmount;
                    currencyAccount.Amount += transaction.TransactionCost - transaction.TransactionComission;
                    _db.ShareAccounts.Update(shareAcc);
                }

                _db.SaveChanges();

                MainViewModel main = new MainViewModel()
                {
                    ShareAccounts = shareAccounts,
                    Shares = shares,
                    Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
                };

                return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", main) });
            }

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

            var sharesAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;
            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;

            PortfolioViewModel portfolioViewModel = new PortfolioViewModel(portfolio.PortfolioId, sharesAccounts, currencyAccounts, transactions);
            portfolioViewModel.PortfolioName = portfolio.PortfolioName;
            portfolioViewModel.Commission = portfolio.Commission;

            var mainportfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            MainViewModel main = new MainViewModel()
            {
                PortfolioName = portfolioViewModel.PortfolioName,
                Value = portfolioViewModel.Value,
                ShareValue = portfolioViewModel.ShareValue,
                Profit = portfolioViewModel.Profit,
                Portfolios = mainportfolios,
                ShareAccounts = sharesAccounts,
                CurrencyAccounts = currencyAccounts,
                Transactions = transactions,
            };

            return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", main) });
        }

        [HttpGet]
        public IActionResult Withdraw(int id)
        {
            CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);

            var currencyAccount = from c in _db.CurrencyAccounts where c.CurrencyAccountId == id select c;

            currencyAccountModel.CurrencyAccount = currencyAccount.First();

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
                return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Withdraw", currencyAccountModel) });
            }

            if (currencyAccounts.First().Amount < currencyAccount.Amount)
            {
                ModelState.AddModelError("", "Недостаточно средств.");
                CurrencyAccountModel currencyAccountModel = new CurrencyAccountModel(_db.Currencies);
                return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Withdraw", currencyAccountModel) });
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

            var sharesAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;
            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;

            PortfolioViewModel portfolioViewModel = new PortfolioViewModel(portfolio.PortfolioId, sharesAccounts, currencyAccounts, transactions);
            portfolioViewModel.PortfolioName = portfolio.PortfolioName;
            portfolioViewModel.Commission = portfolio.Commission;

            var mainportfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            MainViewModel main = new MainViewModel()
            {
                PortfolioName = portfolioViewModel.PortfolioName,
                Value = portfolioViewModel.Value,
                ShareValue = portfolioViewModel.ShareValue,
                Profit = portfolioViewModel.Profit,
                Portfolios = mainportfolios,
                ShareAccounts = sharesAccounts,
                CurrencyAccounts = currencyAccounts,
                Transactions = transactions,
            };

            return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", main) });
        }
        [HttpGet]
        public IActionResult Delete()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

            return View(portfolios.First());
        }

        [HttpPost]
        public IActionResult Delete(int ?id)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;

            _db.Portfolios.Remove(portfolios.First());

            _db.SaveChanges();

            var ps = from p in _db.Portfolios where p.UserId == user.Id select p;
            ps.First().IsDefault = true;

            _db.SaveChanges();

            MainViewModel main = new MainViewModel()
            {
                Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
                Portfolio = ps.First(),
            };

            return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this, "Index", main) });
        }
    }
}
