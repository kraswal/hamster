using hamster.Data;
using hamster.Models.Entities;
using hamsterModel;
using hamsterModel.Logic;
using hamsterModel.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hamster.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public AnalyticsController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Analytics2()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            Portfolio portfolio = portfolios.First();

            var shareAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;

            var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId select c;

            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;

            AnalyticsViewModel analytics = new AnalyticsViewModel()
            {
                PortfolioName = portfolio.PortfolioName,
                PortfolioValue = Logic.PortfolioValueCalculate(shareAccounts, currencyAccounts),
            };

            foreach (var transaction in transactions)
            {
                if (transaction.TransactionDate >= DateTime.Now.AddDays(-30))
                {
                    analytics.TradeVolume += transaction.RubleRateTrade;
                    analytics.CommissionVolume += transaction.RubleRateCommission;
                }
            }

            analytics.PortfolioCommission = analytics.CommissionVolume;

            if (analytics.TradeVolume > 0)
            {


                analytics.PortfolioCommission = Math.Round(analytics.CommissionVolume / analytics.TradeVolume, 5);
                analytics.PortfolioCosts = analytics.CommissionVolume + portfolio.Cost; // плата за комиссии и обслуживание сейчас
                analytics.TariffCost = portfolio.Cost;

                // подбор тарифа

                int tariffId = 0;
                decimal bufferCosts = analytics.PortfolioCosts;
                decimal bufferCommissionVolume = analytics.CommissionVolume;
                var tariffs = from t in _db.Tariffs select t;

                foreach (var tariff in tariffs)
                {
                    if (tariff.QualificationRequirement == false)
                    {
                        decimal tariffCosts = 0;
                        int bufferCost = 0;

                        tariffCosts =  Convert.ToDecimal(tariff.Commission) * analytics.TradeVolume;

                        if (tariff.Condition1 != 0)
                        {
                            if (analytics.PortfolioValue >= tariff.Condition1)
                            {
                                if (analytics.PortfolioValue >= tariff.Condition1)
                                {
                                    bufferCost = 0;
                                }
                            }
                            else
                            {
                                tariffCosts = tariff.Cost + Convert.ToDecimal(tariff.Commission) * analytics.TradeVolume;
                                bufferCost = tariff.Cost;
                            }
                        }
                        

                        // проверяем дешевли ли стоимость обслуживания с этим тарифом

                        if (tariffCosts < bufferCosts)
                        {
                            bufferCosts = tariffCosts;
                            bufferCommissionVolume = Convert.ToDecimal(tariff.Commission) * analytics.TradeVolume;
                            tariffId = tariff.TariffId;
                            analytics.TariffCost = bufferCost;
                            if (tariff.Condition1 != 0 && analytics.PortfolioValue >= tariff.Condition1)
                            analytics.Condition = "При стоимости портфеля больше " + tariff.Condition1 + " млн рублей, обслуживание тарифа бесплатно.";
                        }
                    }
                }

                foreach (var tariff in tariffs)
                {
                    if (tariff.TariffId == tariffId)
                    {
                        analytics.Tariff = tariff;
                        analytics.SuitablePayment = bufferCosts;
                        analytics.SuitableCommissionVolume = bufferCommissionVolume;
                    }
                }
            }


            return View(analytics);
        }

        public IActionResult Analytics()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();

            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            Portfolio portfolio = portfolios.First();


            var mainportfolios = from p in _db.Portfolios where p.UserId == user.Id select p;

            MainViewModel main = new MainViewModel()
            {
                Portfolios = mainportfolios,
            };

            return View(main);
        }


        public IActionResult Tariff()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();
            var portfolios = from p in _db.Portfolios where p.UserId == user.Id && p.IsDefault == true select p;
            Portfolio portfolio = portfolios.First();

            var shareAccounts = from s in _db.ShareAccounts where s.PortfolioId == portfolio.PortfolioId select s;
            var currencyAccounts = from c in _db.CurrencyAccounts where c.PortfolioId == portfolio.PortfolioId select c;
            var transactions = from t in _db.Transactions where t.PortfolioId == portfolio.PortfolioId select t;
            var tariffs = from t in _db.Tariffs select t;

            AnalyticsViewModel analytics =  TinkoffLogic.MinCosts(transactions, shareAccounts, currencyAccounts, portfolio, tariffs);

            return View(analytics);
        }
    }
}
