using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using NewVK.Data;
using NewVK.Security;
using NewVK.Services;

namespace NewVK
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages();

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "newvk_auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                    options.LoginPath = "/Index";
                    options.AccessDeniedPath = "/Index";

                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<AppDbConnectionFactory>();
            builder.Services.AddScoped<UsersRepository>();
            builder.Services.AddScoped<CurrentUserService>();
            builder.Services.AddSingleton<PasswordHasher>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}