using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using hamster.Models.Entities;
using hamsterModel;

namespace hamster.Data
{
    public class AppUserDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public AppUserDbContext(DbContextOptions<AppUserDbContext> options) : base(options)
        {

        }
    }


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<CurrencyAccount> CurrencyAccounts { get; set; }
        public DbSet<ShareAccount> ShareAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<Broker> Brokers { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
    }
}
