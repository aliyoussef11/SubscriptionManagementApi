using Microsoft.EntityFrameworkCore;
using SubscriptionManagementApi.Data;

namespace SubscriptionManagementApi.Services
{
    public class DatabaseManagementService
    {
        public static void MigrationInitialState(IApplicationBuilder app)
        {
            using(var serviceScope = app.ApplicationServices.CreateScope())
            {
                serviceScope.ServiceProvider.GetService<ApplicationDbContext>().Database.Migrate();
            }
        }
    }
}
