using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NewVK.Pages
{
    public class ProfileModel : PageModel
    {
        private const string UserCookieName = "demo_registered_user";
        private const string AuthCookieName = "demo_auth_user";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public string Login { get; private set; } = "";
        public string FirstName { get; private set; } = "";
        public string LastName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Phone { get; private set; } = "";

        public IActionResult OnGet()
        {
            var user = ReadAuthorizedUser();
            if (user is null)
                return RedirectToPage("/Index");

            Login = user.Login;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Phone = user.Phone;

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            Response.Cookies.Delete(AuthCookieName, new CookieOptions { Path = "/" });
            return RedirectToPage("/Index");
        }

        private StoredUser? ReadAuthorizedUser()
        {
            if (!Request.Cookies.TryGetValue(AuthCookieName, out var authLogin) || string.IsNullOrWhiteSpace(authLogin))
                return null;

            if (!Request.Cookies.TryGetValue(UserCookieName, out var json) || string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                var user = JsonSerializer.Deserialize<StoredUser>(json, JsonOptions);
                if (user is null)
                    return null;

                return string.Equals(user.Login, authLogin, StringComparison.OrdinalIgnoreCase)
                    ? user
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private sealed class StoredUser
        {
            public string Login { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
        }
    }
}