using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamsterModel;
using hamster.Models.Entities;
using hamsterModel.ViewModels;
using hamster.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace hamster.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public TransactionController(UserManager<AppUser> userManager,  AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            // получаем данные текущего соединения (пользователя)
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            double MonthProfit = 0, TradeVolume = 0;
            double Commission = 0;
            double monthCommission = 0, monthTradeVolume = 0;

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            var portfolio = portfolios.First();

            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;
            List<CurrencyPair> currencyPairs = Shares500.ReadCurrencyPairs();

            foreach (var transaction in transactions)
            {
                if (transaction.PortfolioId == portfolio.PortfolioId)
                {
                    if (transaction.TransactionType == TransactionType.Покупка || transaction.TransactionType == TransactionType.Продажа)
                    {
                        foreach (var currencyPair in currencyPairs)
                        {
                            if (transaction.CurrencyTicker != "RUB")
                            {
                                if (transaction.CurrencyTicker == currencyPair.BuyCurrency && "RUB" == currencyPair.PayCurrency)
                                {
                                    Commission += Convert.ToDouble(transaction.TransactionComission * currencyPair.Rate);
                                    TradeVolume += Convert.ToDouble(transaction.TransactionCost * currencyPair.Rate);
                                    if (transaction.TransactionDate >= DateTime.Now.AddDays(-30))
                                    {
                                        monthCommission += Convert.ToDouble(transaction.TransactionComission * currencyPair.Rate);
                                        monthTradeVolume += Convert.ToDouble((transaction.TransactionCost) * currencyPair.Rate);
                                        MonthProfit += Convert.ToDouble(transaction.TradeResult * currencyPair.Rate);
                                    }
                                }
                            }
                            else
                            {
                                Commission += Convert.ToDouble(transaction.TransactionComission);
                                TradeVolume += Convert.ToDouble(transaction.TransactionCost);
                                if (transaction.TransactionDate >= DateTime.Now.AddDays(-30))
                                {
                                    monthCommission += Convert.ToDouble(transaction.TransactionComission);
                                    monthTradeVolume += Convert.ToDouble(transaction.TransactionCost);
                                    MonthProfit += Convert.ToDouble(transaction.TradeResult);
                                }
                            }
                        }
                    }
                }
            }
            TransactionViewModel transactionViewModel = new TransactionViewModel(transactions, Commission, MonthProfit, TradeVolume, monthCommission, monthTradeVolume);
            _db.SaveChanges();

            MainViewModel main = new MainViewModel()
            {
                Transactions = transactions,
                TradeVolume = transactionViewModel.TradeVolume,
                Portfolios = from p in _db.Portfolios where p.UserId == user.Id select p,
            };

            return View(main);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                transaction.TransactionCost = Convert.ToDecimal(transaction.TransactionAmount * transaction.TransactionPrice + transaction.TransactionComission);
                _db.Transactions.Add(transaction);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }     
            return View();
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var transaction = _db.Transactions.Find(id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Transaction trans)
        {
            if (trans == null)
            {
                return NotFound();
            }
            else
            {
                var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

                var portfolios = from p in _db.Portfolios where p.UserId == user.Id select p;
                var portfolio = portfolios.First();


                var transaction = _db.Transactions.Find(trans.TransactionId);
                var transactions = from t in _db.Transactions where t.AssetName == transaction.AssetName && t.PortfolioId == portfolio.PortfolioId select t;

                var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId && c.CurrencyTicker == transaction.CurrencyTicker select c;

                if (transaction.TransactionId == transactions.OrderBy(t => t.TransactionId).Last().TransactionId) // последняя транзакция по активу
                {
                    if (transaction.TransactionType == TransactionType.Покупка)
                    {
                        foreach (var shareAccount in _db.ShareAccounts)
                        {
                            if (shareAccount.ShareName == transaction.AssetName)
                            {
                                if (shareAccount.Amount == transaction.TransactionAmount)
                                {
                                    _db.ShareAccounts.Remove(shareAccount);
                                }
                                else
                                {
                                    shareAccount.AveragePrice = (shareAccount.AveragePrice * shareAccount.Amount - transaction.TransactionPrice * transaction.TransactionAmount) / (shareAccount.Amount - transaction.TransactionAmount);
                                    shareAccount.Amount -= transaction.TransactionAmount;
                                }
                                if (transaction.Internal == true)
                                {
                                    currencyAccounts.First().Amount += transaction.TransactionCost + transaction.TransactionComission;
                                }
                            }
                        }
                    }
                    if (transaction.TransactionType == TransactionType.Продажа)
                    {
                        var shareAccounts = from s in _db.ShareAccounts where (s.ShareName == transaction.AssetName) select s;
                        if (shareAccounts.Count() != 0)
                        {
                            shareAccounts.First().Amount += transaction.TransactionAmount;
                        }
                        else
                        {
                            if (transaction.TradeResult >= 0)
                            {
                                _db.ShareAccounts.AddRange(new ShareAccount
                                {
                                    ShareName = transaction.AssetName,
                                    ShareTicker = transaction.AssetTicker,
                                    Amount = transaction.TransactionAmount,
                                    AveragePrice = Convert.ToDecimal((transaction.TransactionCost - transaction.TransactionComission - transaction.TradeResult) / transaction.TransactionAmount),
                                    CurrencyName = transaction.CurrencyName,
                                    CurrencySign = transaction.CurrencySign,

                                });
                            }
                            else
                            {
                                _db.ShareAccounts.AddRange(new ShareAccount
                                {
                                    ShareName = transaction.AssetName,
                                    ShareTicker = transaction.AssetTicker,
                                    Amount = transaction.TransactionAmount,
                                    AveragePrice = Convert.ToDecimal((transaction.TransactionCost - transaction.TransactionComission + transaction.TradeResult) / transaction.TransactionAmount),
                                    CurrencyName = transaction.CurrencyName,
                                    CurrencySign = transaction.CurrencySign,

                                });
                            }
                        }

                        if (currencyAccounts.Count() != 0)
                        {
                            currencyAccounts.First().Amount -= transaction.TransactionCost;
                            currencyAccounts.First().Amount += transaction.TransactionComission;

                            if (currencyAccounts.First().Amount == 0)
                            {
                                _db.CurrencyAccounts.Remove(currencyAccounts.First());
                            }
                        }

                    }
                    if (transaction.TransactionType == TransactionType.Пополнение)
                    {
                        foreach (var curAcc in _db.CurrencyAccounts)
                        {
                            if (curAcc.CurrencyTicker == transaction.AssetTicker)
                            {
                                if (curAcc.Amount == transaction.TransactionAmount)
                                {
                                    _db.CurrencyAccounts.Remove(curAcc);
                                }
                                else
                                {
                                    curAcc.Amount -= transaction.TransactionAmount;
                                }
                            }
                        }
                    }
                    if (transaction.TransactionType == TransactionType.Вывод)
                    {
                        if (currencyAccounts.Count() != 0)
                        {
                            currencyAccounts.First().Amount += transaction.TransactionAmount;
                        }
                        else
                        {
                            _db.CurrencyAccounts.AddRange(new CurrencyAccount
                            {
                                CurrencyName = transaction.CurrencyName,
                                CurrencyTicker = transaction.AssetTicker,
                                CurrencySign = transaction.CurrencySign,
                                Amount = transaction.TransactionAmount,
                            });
                        }
                    }

                    _db.Transactions.Remove(transaction);
                    _db.SaveChanges();


                    return RedirectToAction("Index");
                }
                else
                {
                    return Content("Нельзя удалить не последнюю операцию по активу");
                }
            }
        }

        [HttpGet]
        public IActionResult Update(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var transaction = _db.Transactions.Find(id);
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Transaction transaction)
        {
            var transactions = from t in _db.Transactions where t.TransactionId == transaction.TransactionId select t;
            if (transactions.First().Internal == true)
            {
                var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == transactions.First().PortfolioId && c.CurrencyTicker == transactions.First().CurrencyTicker select c;
                var currency = currencyAccounts.First();

                if (currency.Amount >= (transaction.TransactionComission - transactions.First().TransactionComission))
                {
                    if (transaction.TransactionComission > transactions.First().TransactionComission)
                    {
                        currency.Amount -= transaction.TransactionComission - transactions.First().TransactionComission;
                        transactions.First().TransactionComission = transaction.TransactionComission;
                    }
                    else
                    {
                        currency.Amount += transaction.TransactionComission - transactions.First().TransactionComission;
                        transactions.First().TransactionComission = transaction.TransactionComission;
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Недостаточно средств.");
                    return View(transaction);
                }
            }
            else
            {
                transactions.First().TransactionComission = transaction.TransactionComission;
            }

            if (transactions.First().TransactionType == TransactionType.Продажа)
            {
                if (transaction.TransactionComission < transactions.First().TransactionComission)
                {
                    var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == transactions.First().PortfolioId && c.CurrencyTicker == transactions.First().CurrencyTicker select c;
                    var currencyAccount = currencyAccounts.First();

                    currencyAccount.Amount += transactions.First().TransactionComission - transaction.TransactionComission;
                }
            }
            

            _db.SaveChanges();
            return RedirectToAction("Index");

        }
    }
}
