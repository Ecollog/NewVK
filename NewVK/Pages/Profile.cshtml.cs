using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        public ProfileModel(UsersRepository usersRepository, CurrentUserService currentUserService)
        {
            _usersRepository = usersRepository;
            _currentUserService = currentUserService;
        }

        public string Login { get; private set; } = "";
        public string FirstName { get; private set; } = "";
        public string LastName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Phone { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();

            if (userId is null)
                return RedirectToPage("/Index");

            AppUser? user = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);

            if (user is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Index");
            }

            Login = user.Login;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Phone = user.Phone;

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }
    }
}