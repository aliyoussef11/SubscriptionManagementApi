using SubscriptionManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace SubscriptionManagementApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<RemainingDaysResult> RemainingDaysResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RemainingDaysResult>().HasNoKey();
        }

        public IEnumerable<Subscription> GetActiveSubscriptions()
        {
            var activeSubscriptions = this.Set<Subscription>()
            .FromSqlInterpolated($@"
                SELECT
                    *
                FROM get_active_subscriptions()")
            .ToList();

            return activeSubscriptions;
        }

        public IEnumerable<Subscription> GetSubscriptionsByUser(int userId)
        {
            var subscriptions = this.Set<Subscription>()
             .FromSqlInterpolated($@"
                SELECT
                    *
                FROM get_subscriptions_by_user({userId})")
             .ToList();

            return subscriptions;
        }

        public int CalculateRemainingDays(int subscriptionId)
        {
            // var parameters = new NpgsqlParameter("@subscription_id", subscriptionId);

            var result = this.RemainingDaysResults
            .FromSqlRaw($"SELECT calculate_remaining_days({subscriptionId}) as \"RemainingDays\"")
            .FirstOrDefault();

            return result != null ? result.RemainingDays : -1;
        }
    }
}
