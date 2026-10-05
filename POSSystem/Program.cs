using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Configure EF Core with SQL Server LocalDB
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. Add ASP.NET Core Identity with Role Support (Admin, Manager, Cashier)
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                // Simple password rules for easy lab demo
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;

                // Ensure unique emails across all staff accounts
                options.User.RequireUniqueEmail = true;

                // No email confirmation required
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // Set the login and access denied paths
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // 3. Add MVC Controllers and Views
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // 4. Automatically ensure database tables exist and seed roles on startup
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                    context.Database.Migrate();

                    // Seed Identity roles in AspNetRoles table (Admin, Manager, Cashier)
                    foreach (var roleName in Enum.GetNames<UserRole>())
                    {
                        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
                        {
                            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while setting up the database.");
                }
            }

            // 5. Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Authentication must come BEFORE Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            // Required for Identity Razor Pages (Login, Register, Logout)
            app.MapRazorPages();

            app.Run();
        }
    }
}
