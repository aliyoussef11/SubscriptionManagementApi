using SubscriptionManagementApi.Data;
using SubscriptionManagementApi.Models;

namespace SubscriptionManagementApi.Repositories
{
    public class SubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Subscription> GetSubscriptionsByUserId(int userId)
        {
            return _context.GetSubscriptionsByUser(userId).ToList();
        }

        public List<Subscription> GetActiveSubscriptions()
        {
            return _context.GetActiveSubscriptions().ToList();
        }

        public int CalculateRemainingDays(int subscriptionId)
        {
            return _context.CalculateRemainingDays(subscriptionId);
        }
    }
}
