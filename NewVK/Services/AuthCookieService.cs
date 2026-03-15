using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NewVK.Models;

namespace NewVK.Services
{
    public sealed class AuthCookieService
    {
        public const string ThemeClaimType = "theme_key";

        public async Task SignInAsync(HttpContext httpContext, AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ThemeClaimType, string.IsNullOrWhiteSpace(user.ThemeKey) ? SiteThemeDefaults.DefaultKey : user.ThemeKey)
            };

            if (!string.IsNullOrWhiteSpace(user.Phone))
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone));

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true,
                ExpiresUtc = isPersistent
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : null
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);
        }

        public Task SignOutAsync(HttpContext httpContext)
        {
            return httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}