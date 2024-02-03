using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionManagementApi.Migrations
{
    public partial class AddFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // calculate remaining days
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION calculate_remaining_days(subscription_id INTEGER)
                RETURNS INTEGER
                AS $$
                DECLARE
                    RemainingDays INTEGER;
                BEGIN
                    SELECT EXTRACT(DAYS FROM (""EndDate"" - CURRENT_DATE)) 
                    INTO RemainingDays
                    FROM public.""Subscriptions""
                    WHERE ""SubscriptionId"" = calculate_remaining_days.subscription_id;

                    RETURN RemainingDays;
                END;
                $$
                LANGUAGE plpgsql;
            ");

            // get subscriptions by user
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION get_subscriptions_by_user(user_id INTEGER)
                RETURNS TABLE (
                    SubscriptionId INTEGER,
                    UserId INTEGER,
                    StartDate TIMESTAMP WITH TIME ZONE,
                    EndDate TIMESTAMP WITH TIME ZONE,
                    SubscriptionType VARCHAR(255)
                )
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT
                        s.""SubscriptionId"",
                        s.""UserId"",
                        s.""StartDate"",
                        s.""EndDate"",
                        s.""SubscriptionType""::VARCHAR(255)
                    FROM
                        public.""Subscriptions"" s
                    WHERE
                        s.""UserId"" = get_subscriptions_by_user.user_id;
                END;
                $$
                LANGUAGE plpgsql;
            ");

            // get active subscriptions
            migrationBuilder.Sql(@"            
                CREATE OR REPLACE FUNCTION get_active_subscriptions()
                RETURNS TABLE (
                    SubscriptionId INTEGER,
                    UserId INTEGER,
                    StartDate TIMESTAMP WITH TIME ZONE,
                    EndDate TIMESTAMP WITH TIME ZONE,
                    SubscriptionType VARCHAR(255)
                )
                AS $$
                BEGIN
                    RETURN QUERY
                    SELECT
                        s.""SubscriptionId"",
                        s.""UserId"",
                        s.""StartDate"",
                        s.""EndDate"",
                        s.""SubscriptionType""::VARCHAR(255)
                    FROM
                        public.""Subscriptions"" s
                    WHERE
                        s.""StartDate"" <= CURRENT_DATE
                        AND s.""EndDate"" >= CURRENT_DATE;
                END;
                $$
                LANGUAGE plpgsql;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
