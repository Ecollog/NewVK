using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewVK.Data;
using NewVK.Models;
using NewVK.Services;

namespace NewVK.Pages
{
    [Authorize]
    public class EditProfileModel : PageModel
    {
        private readonly UsersRepository _usersRepository;
        private readonly CurrentUserService _currentUserService;
        private readonly ThemeCatalogService _themeCatalogService;
        private readonly AuthCookieService _authCookieService;

        public EditProfileModel(
            UsersRepository usersRepository,
            CurrentUserService currentUserService,
            ThemeCatalogService themeCatalogService,
            AuthCookieService authCookieService)
        {
            _usersRepository = usersRepository;
            _currentUserService = currentUserService;
            _themeCatalogService = themeCatalogService;
            _authCookieService = authCookieService;
        }

        [BindProperty]
        public EditProfileVm Input { get; set; } = new();

        public IReadOnlyList<SiteTheme> Themes { get; private set; } = Array.Empty<SiteTheme>();

        public string SuccessMessage { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();

            if (userId is null)
                return RedirectToPage("/Index");

            AppUser? user = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);

            if (user is null)
                return RedirectToPage("/Index");

            Themes = _themeCatalogService.GetAll();

            Input = new EditProfileVm
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                AboutMe = user.AboutMe ?? "",
                ThemeKey = string.IsNullOrWhiteSpace(user.ThemeKey) ? SiteThemeDefaults.DefaultKey : user.ThemeKey
            };

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
        {
            Themes = _themeCatalogService.GetAll();

            int? userId = _currentUserService.GetUserId();

            if (userId is null)
                return RedirectToPage("/Index");

            ValidateInput();

            if (!_themeCatalogService.Exists(Input.ThemeKey))
            {
                ModelState.AddModelError("Input.ThemeKey", "Выберите корректную тему.");
            }

            if (await _usersRepository.LoginExistsForOtherAsync(userId.Value, Input.Login, cancellationToken))
            {
                ModelState.AddModelError("Input.Login", "Этот логин уже занят.");
            }

            if (await _usersRepository.EmailExistsForOtherAsync(userId.Value, Input.Email, cancellationToken))
            {
                ModelState.AddModelError("Input.Email", "Этот email уже используется.");
            }

            if (!ModelState.IsValid)
                return Page();

            var request = new UpdateUserProfileRequest
            {
                Id = userId.Value,
                Login = Input.Login.Trim(),
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                Email = Input.Email.Trim(),
                Phone = Input.Phone.Trim(),
                AboutMe = string.IsNullOrWhiteSpace(Input.AboutMe) ? null : Input.AboutMe.Trim(),
                ThemeKey = Input.ThemeKey
            };

            await _usersRepository.UpdateProfileAsync(request, cancellationToken);

            AppUser? updatedUser = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);

            if (updatedUser is not null)
            {
                await _authCookieService.SignInAsync(HttpContext, updatedUser, isPersistent: true);
            }

            SuccessMessage = "Изменения сохранены.";
            return Page();
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Input.Login))
                ModelState.AddModelError("Input.Login", "Введите логин.");

            if (string.IsNullOrWhiteSpace(Input.FirstName))
                ModelState.AddModelError("Input.FirstName", "Введите имя.");

            if (string.IsNullOrWhiteSpace(Input.LastName))
                ModelState.AddModelError("Input.LastName", "Введите фамилию.");

            if (string.IsNullOrWhiteSpace(Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Введите email.");
            }
            else if (!new EmailAddressAttribute().IsValid(Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Некорректный email.");
            }

            if (!string.IsNullOrWhiteSpace(Input.Phone) && !new PhoneAttribute().IsValid(Input.Phone))
                ModelState.AddModelError("Input.Phone", "Некорректный телефон.");

            if (!string.IsNullOrWhiteSpace(Input.Login) && Input.Login.Trim().Length < 3)
                ModelState.AddModelError("Input.Login", "Логин должен содержать минимум 3 символа.");

            if (!string.IsNullOrWhiteSpace(Input.AboutMe) && Input.AboutMe.Length > 1000)
                ModelState.AddModelError("Input.AboutMe", "Блок \"О себе\" не должен превышать 1000 символов.");
        }

        public sealed class EditProfileVm
        {
            [Display(Name = "Логин")]
            public string Login { get; set; } = "";

            [Display(Name = "Имя")]
            public string FirstName { get; set; } = "";

            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = "";

            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Display(Name = "Телефон")]
            public string Phone { get; set; } = "";

            [Display(Name = "О себе")]
            public string AboutMe { get; set; } = "";

            [Display(Name = "Цветовая схема")]
            public string ThemeKey { get; set; } = SiteThemeDefaults.DefaultKey;
        }
    }
}