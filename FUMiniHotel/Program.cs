using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.Repositories.IRepositories;
using FUMiniHotel.DAO.Data;
using FUMiniHotel.Repositories;
using FUMiniHotel.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using FUMiniHotel.Services;

namespace FUMiniHotel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Connection String
            var connectionString = builder.Configuration.GetConnectionString("FUMiniHotelContextConnection")
                ?? throw new InvalidOperationException("Connection string 'FUMiniHotelContextConnection' not found.");

            // Register DbContext
            builder.Services.AddDbContext<FUMiniHotelContext>(options =>
                options.UseSqlServer(connectionString));

            // Register Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<FUMiniHotelContext>()
            .AddDefaultTokenProviders();

            // Configure authentication
            builder.Services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                    options.Cookie.Name = "FUMiniHotelAuthCookie";
                });

            // Register repositories and services
            builder.Services.AddTransient<IEmailSender, EmailSenderService>();
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IRoomRepository, RoomRepository>();
            builder.Services.AddTransient<IFileService, FileService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Seed roles on app startup
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                foreach (var role in RoleHelper.GetAllRoles())
                {
                    var roleName = role.ToRoleString();
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
            }

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
