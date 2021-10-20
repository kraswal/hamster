using hamster.Models.Entities;
using hamsterModel;
using System;
using System.Linq;

namespace hamster.Data
{
    public static class SampleData
    {
        public static void Initialize(AppDbContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new AppUser
                    {
                        UserName = "admin@mail.ru",
                        Nickname = "Admin",
                        Password = "123",
                        IsAdmin = true,
                    },
                    new AppUser
                    {
                        UserName = "chingis@mail.ru",
                        Nickname = "Chingis",
                        Password = "123",
                        IsAdmin = false,
                    }
                    );
            }   

            if (!context.Currencies.Any())
            {
                context.Currencies.AddRange(
                    new Currency
                    {
                        CurrencyName = "Российский рубль",
                        CurrencySign = "₽",
                        CurrencyTicker = "RUB",
                        CurrencyCountry = "Россия",
                    },
                    new Currency
                    {
                        CurrencyName = "Доллар США",
                        CurrencySign = "$",
                        CurrencyTicker = "USD",
                        CurrencyCountry = "Соединенные штаты Америки",
                    },
                    new Currency
                    {
                        CurrencyName = "Евро",
                        CurrencySign = "€",
                        CurrencyTicker = "EUR",
                        CurrencyCountry = "Европа",
                    },
                    new Currency
                    {
                        CurrencyName = "Китайский юань",
                        CurrencySign = "¥",
                        CurrencyTicker = "CNY",
                        CurrencyCountry = "Китай",
                    }
                    );
            }

            if (!context.Brokers.Any())
            {
                context.Brokers.AddRange(
                    new Broker
                    {
                        BrokerName = "Тинькофф",
                    },
                    new Broker
                    {
                        BrokerName = "Сбербанк",
                    },
                    new Broker
                    {
                        BrokerName = "БКС Брокер",
                    }
                    );
            }

            context.SaveChanges();

            if (!context.Tariffs.Any())
            {
                context.Tariffs.AddRange(
                    new Tariff
                    {
                        BrokerId = 1,
                        BrokerName = context.Brokers.Find(1).BrokerName,
                        TariffName = "Инвестор",
                        Info = "0 ₽ всегда",
                        Commission = 0.003,
                        Cost = 0,


                    },
                    new Tariff
                    {
                        BrokerId = 1,
                        BrokerName = context.Brokers.Find(1).BrokerName,
                        TariffName = "Трейдер",
                        Info = "0 ₽ /n если не торгуете",
                        Commission = 0.0005 ,
                        Cost = 290,
                        Condition1 = 2000000,
                    },
                    new Tariff
                    {
                        BrokerId = 1,
                        BrokerName = context.Brokers.Find(1).BrokerName,
                        TariffName = "Премиум",
                        Info = "",
                        Commission = 0.00025,
                        Cost = 3000,
                        CostCondition2 = 990,
                        Condition1 = 3000000,
                        Condition2 = 1000000,
                    },
                    new Tariff
                    {
                        BrokerId = 2,
                        BrokerName = context.Brokers.Find(2).BrokerName,
                        TariffName = "Инвестиционный",
                        Info = "",
                        Commission = 0.003,
                        Cost = 0,
                    },
                    new Tariff
                    {
                        BrokerId = 2,
                        BrokerName = context.Brokers.Find(2).BrokerName,
                        TariffName = "Самостоятельный",
                        Info = "",
                        Commission = 0.0006,
                        Cost = 0,
                        QualificationRequirement = true,
                    },
                    new Tariff
                    {
                        BrokerId = 3,
                        BrokerName = context.Brokers.Find(3).BrokerName,
                        TariffName = "Инвестор",
                        Info = "Для клиентов, которые совершают небольшое количество сделок.",
                        Commission = 0.001,
                        Cost = 0,
                    }
                    );
            }


            context.SaveChanges();
        }
    }
}
