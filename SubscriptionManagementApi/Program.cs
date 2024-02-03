using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SubscriptionManagementApi.Data;
using SubscriptionManagementApi.Repositories;
using SubscriptionManagementApi.Services;
using System.Text;

namespace SubscriptionManagementApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<SubscriptionRepository>();
            services.AddScoped<AuthenticationService>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Configure Serilog for logging into file
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/subscriptionManagementApi.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddLogging(logging =>
            {
                logging.AddSerilog(Log.Logger);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"], 
                    ValidAudience = Configuration["Jwt:Audience"], 
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"])) 
                };
            });

            //var server = Configuration["DatabaseServer"] ?? "";
            //var port = Configuration["DatabasePort"] ?? "";
            //var user = Configuration["DatabaseUser"] ?? "";
            //var password = Configuration["DatabasePassword"] ?? "";
            //var database = Configuration["DatabaseName"] ?? "";

            //var connectionString = $"Server={server}, {port}; Initial Catalog={database}; User ID={user}; Password={password}";

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {          
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                // RUN Migrations
                DatabaseManagementService.MigrationInitialState(app);
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
