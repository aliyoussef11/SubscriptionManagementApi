using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionManagementApi.Migrations
{
    public partial class SeedUserData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO public.\"Users\"(\"Username\", \"PasswordHash\", \"Email\")\r\n\tVALUES  ('test_user', 'hashed_password_here', 'test_user@test.com');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
