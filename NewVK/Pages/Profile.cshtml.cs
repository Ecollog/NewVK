using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewVK.Data;
using NewVK.Models;
using NewVK.Services;

namespace NewVK.Pages
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UsersRepository _usersRepository;
        private readonly CurrentUserService _currentUserService;
        private readonly AuthCookieService _authCookieService;

        public ProfileModel(
            UsersRepository usersRepository,
            CurrentUserService currentUserService,
            AuthCookieService authCookieService)
        {
            _usersRepository = usersRepository;
            _currentUserService = currentUserService;
            _authCookieService = authCookieService;
        }

        public string Login { get; private set; } = "";
        public string FirstName { get; private set; } = "";
        public string LastName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Phone { get; private set; } = "";
        public string AboutMe { get; private set; } = "";
        public string ThemeKey { get; private set; } = SiteThemeDefaults.DefaultKey;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();

            if (userId is null)
                return RedirectToPage("/Index");

            AppUser? user = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);

            if (user is null)
            {
                await _authCookieService.SignOutAsync(HttpContext);
                return RedirectToPage("/Index");
            }

            Login = user.Login;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Phone = user.Phone;
            AboutMe = user.AboutMe ?? "";
            ThemeKey = user.ThemeKey;

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _authCookieService.SignOutAsync(HttpContext);
            return RedirectToPage("/Index");
        }
    }
}