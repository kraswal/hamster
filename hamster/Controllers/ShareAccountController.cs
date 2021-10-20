using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamsterModel;
using hamsterModel.ViewModels;
using hamster.Data;
using hamster.Models;
using hamsterModel.Logic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using hamster.Models.Entities;

namespace hamster.Controllers
{
    [Authorize]
    public class ShareAccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        public ShareAccountController(UserManager<AppUser> userManager,  AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // 100%
        public IActionResult Index()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();
            
            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            Portfolio portfolio = portfolios.First();

            var sharesAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;

            ShareAccountViewModel shareAccountViewModel = new ShareAccountViewModel(sharesAccounts);

            List<CurrencyPair> currencyPairs = Shares500.ReadCurrencyPairs();
            foreach (var shareAccount in sharesAccounts)
            {
                foreach (var share in shareAccountViewModel.Shares)
                {
                    if (shareAccount.ShareTicker == share.Ticker)
                    {
                        if (shareAccount.CurrencyTicker != "RUB")
                        {
                            foreach (var currencyPair in currencyPairs)
                            {
                                if (shareAccount.CurrencyTicker == currencyPair.BuyCurrency && "RUB" == currencyPair.PayCurrency)
                                {
                                    shareAccountViewModel.Value += shareAccount.Amount * share.Price * currencyPair.Rate;
                                    shareAccountViewModel.Profit += (share.Price * shareAccount.Amount - shareAccount.AveragePrice * shareAccount.Amount) * currencyPair.Rate;
                                }
                            }
                        }
                        else
                        {
                            shareAccountViewModel.Value += shareAccount.Amount * share.Price;
                            shareAccountViewModel.Profit += share.Price * shareAccount.Amount - shareAccount.AveragePrice * shareAccount.Amount;
                        }
                    }          
                }
            }

            MainViewModel main = new MainViewModel()
            {
                Value = shareAccountViewModel.Value,
                Profit = shareAccountViewModel.Profit,
                ShareAccounts = shareAccountViewModel.ShareAccounts,
                Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
            };

            return View(main);
        }

        [HttpGet]
        [NoDirectAccess]
        public IActionResult Buy()
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
        public IActionResult Buy(Transaction transaction)
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
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
                }

                var currencyAccount = currencyAccounts.First();

                if (currencyAccount.Amount < transaction.TransactionCost)
                {
                    ModelState.AddModelError("", "Недостаточно средств.");
                    ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                    shareAccountModel.Transaction = transaction;
                    ViewBag.DefaultCommission = portfolio.Commission;
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
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
            };

            return Json(new { isValid = true, html = Helper.RenderRazorViewToString(this,"Index", main) });
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
                        return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
                    }

                    var currencyAccount = currencyAccounts.First();

                    if (currencyAccount.Amount < transaction.TransactionCost)
                    {
                        ModelState.AddModelError("", "Недостаточно средств.");
                        ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                        shareAccountModel.Transaction = transaction;
                        ViewBag.DefaultCommission = portfolio.Commission;
                        return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
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
                var shareAccount = shareAccounts.First();

                // проверка на наличие таких акций и их количество

                if (shareAccounts.Count() == 0)
                {
                    ModelState.AddModelError("", "В портфеле нет акций " + transaction.AssetName);
                    ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                    ViewBag.DefaultCommission = portfolio.Commission;
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
                }

                if (shareAccount.Amount < transaction.TransactionAmount)
                {
                    ModelState.AddModelError("", "В портфеле недостаточно акций " + transaction.AssetName);
                    ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                    ViewBag.DefaultCommission = portfolio.Commission;
                    return Json(new { isValid = false, html = Helper.RenderRazorViewToString(this, "Buy", shareAccountModel) });
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
        public IActionResult Sell()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            ViewBag.DefaultCommission = portfolio.Commission;

            IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);
            ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
            return View(shareAccountModel);
        }
        [HttpPost]
        public IActionResult Sell(Transaction transaction)
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);

            var shareAccounts = from s in _db.ShareAccounts where s.ShareName == transaction.AssetName && s.PortfolioId == portfolio.PortfolioId select s;
            var shareAccount = shareAccounts.First();

            // проверка на наличие таких акций и их количество

            if (shareAccounts.Count() == 0)
            {
                ModelState.AddModelError("", "В портфеле нет акций " + transaction.AssetName);
                ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                ViewBag.DefaultCommission = portfolio.Commission;
                return View(shareAccountModel);
            }
            
            if (shareAccount.Amount < transaction.TransactionAmount)
            {
                ModelState.AddModelError("", "В портфеле недостаточно акций " + transaction.AssetName);
                ShareAccountModel shareAccountModel = new ShareAccountModel(shares);
                ViewBag.DefaultCommission = portfolio.Commission;
                return View(shareAccountModel);
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

            return RedirectToAction("Index");
        }

        public IActionResult Details(int ?id)
        {

            if (id == 0)
            {
                return NotFound();
            }

            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;
            var portfolio = portfolios.First();

            IEnumerable<Share> shares = Shares500.ReadShares(_db.Currencies);

            var shareAccounts = from s in _db.ShareAccounts where s.ShareAccountId == id && s.PortfolioId == portfolio.PortfolioId select s;
            var shareAccount = shareAccounts.First();

            ShareAccountViewDetails shareAccountView = new ShareAccountViewDetails(shareAccount);


            return View(shareAccountView);
        }
    }
}
