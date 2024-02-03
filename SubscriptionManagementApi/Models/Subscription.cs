using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionManagementApi.Models
{
    public class Subscription
    {
        public int SubscriptionId { get; set; }

        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SubscriptionType { get; set; }
    }

    public class RemainingDaysResult
    {
        public int RemainingDays { get; set; }
    }
}
