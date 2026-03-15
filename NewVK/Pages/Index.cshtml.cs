using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewVK.Data;
using NewVK.Models;
using NewVK.Security;

namespace NewVK.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly UsersRepository _usersRepository;
        private readonly PasswordHasher _passwordHasher;

        public IndexModel(UsersRepository usersRepository, PasswordHasher passwordHasher)
        {
            _usersRepository = usersRepository;
            _passwordHasher = passwordHasher;
        }

        public string SuccessMessage { get; set; } = "";

        [BindProperty]
        public AuthVm Auth { get; set; } = new() { Mode = "login" };

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Profile");

            Auth.Mode = NormalizeMode(Auth.Mode);
            return Page();
        }

        public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Profile");

            Auth.Mode = NormalizeMode(Auth.Mode);

            if (Auth.Mode == "login")
                ClearRegisterValues();

            ValidateForm();

            if (!ModelState.IsValid)
                return Page();

            return Auth.Mode == "register"
                ? await HandleRegisterAsync(cancellationToken)
                : await HandleLoginAsync(cancellationToken);
        }

        private async Task<IActionResult> HandleRegisterAsync(CancellationToken cancellationToken)
        {
            if (await _usersRepository.LoginExistsAsync(Auth.Login, cancellationToken))
            {
                ModelState.AddModelError("Auth.Login", "Логин уже занят.");
                Auth.Mode = "register";
                return Page();
            }

            if (await _usersRepository.EmailExistsAsync(Auth.Email, cancellationToken))
            {
                ModelState.AddModelError("Auth.Email", "Email уже используется.");
                Auth.Mode = "register";
                return Page();
            }

            var hashResult = _passwordHasher.Hash(Auth.Password);

            var request = new RegisterUserRequest
            {
                Login = Auth.Login.Trim(),
                FirstName = Auth.FirstName.Trim(),
                LastName = Auth.LastName.Trim(),
                Email = Auth.Email.Trim(),
                Phone = Auth.Phone.Trim(),
                AboutMe = null,
                PasswordHash = hashResult.Hash,
                PasswordSalt = hashResult.Salt
            };

            int userId = await _usersRepository.CreateAsync(request, cancellationToken);
            AppUser? user = await _usersRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Не удалось завершить регистрацию.");
                Auth.Mode = "register";
                return Page();
            }

            await SignInAsync(user, isPersistent: true);
            return RedirectToPage("/Profile");
        }

        private async Task<IActionResult> HandleLoginAsync(CancellationToken cancellationToken)
        {
            AppUser? user = await _usersRepository.GetByLoginOrEmailAsync(Auth.Login, cancellationToken);

            if (user is null)
            {
                ModelState.AddModelError("Auth.Login", "Пользователь с таким логином или email не найден.");
                return Page();
            }

            bool passwordOk = _passwordHasher.Verify(Auth.Password, user.PasswordHash, user.PasswordSalt);

            if (!passwordOk)
            {
                ModelState.AddModelError("Auth.Password", "Неверный пароль.");
                return Page();
            }

            await _usersRepository.UpdateLastLoginAsync(user.Id, cancellationToken);
            await SignInAsync(user, Auth.RememberMe);

            return RedirectToPage("/Profile");
        }

        private async Task SignInAsync(AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Email, user.Email)
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

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);
        }

        private void ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(Auth.Login))
                ModelState.AddModelError("Auth.Login", "Введите логин (или email для входа).");

            if (string.IsNullOrWhiteSpace(Auth.Password))
                ModelState.AddModelError("Auth.Password", "Введите пароль.");

            if (Auth.Mode == "login")
                return;

            if (string.IsNullOrWhiteSpace(Auth.FirstName))
                ModelState.AddModelError("Auth.FirstName", "Введите имя.");

            if (string.IsNullOrWhiteSpace(Auth.LastName))
                ModelState.AddModelError("Auth.LastName", "Введите фамилию.");

            if (string.IsNullOrWhiteSpace(Auth.Email))
            {
                ModelState.AddModelError("Auth.Email", "Введите email.");
            }
            else if (!new EmailAddressAttribute().IsValid(Auth.Email))
            {
                ModelState.AddModelError("Auth.Email", "Некорректный email.");
            }

            if (!string.IsNullOrWhiteSpace(Auth.Phone) && !new PhoneAttribute().IsValid(Auth.Phone))
                ModelState.AddModelError("Auth.Phone", "Некорректный телефон.");

            bool passOk = Regex.IsMatch(
                Auth.Password ?? string.Empty,
                @"^(?=.*[A-ZА-ЯЁ])(?=.*[a-zа-яё])(?=.*\d).{8,64}$");

            if (!passOk)
            {
                ModelState.AddModelError(
                    "Auth.Password",
                    "Пароль: 8-64 символов, минимум 1 цифра, 1 строчная и 1 заглавная буква.");
            }

            if (string.IsNullOrWhiteSpace(Auth.ConfirmPassword))
            {
                ModelState.AddModelError("Auth.ConfirmPassword", "Повторите пароль.");
            }
            else if (!string.Equals(Auth.Password, Auth.ConfirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("Auth.ConfirmPassword", "Пароли не совпадают.");
            }

            if (!Auth.AcceptTerms)
                ModelState.AddModelError("Auth.AcceptTerms", "Нужно принять условия.");
        }

        private void ClearRegisterValues()
        {
            Auth.FirstName = "";
            Auth.LastName = "";
            Auth.Email = "";
            Auth.Phone = "";
            Auth.ConfirmPassword = "";
            Auth.AcceptTerms = false;
        }

        private static string NormalizeMode(string? mode)
            => mode == "register" ? "register" : "login";

        public sealed class AuthVm
        {
            [HiddenInput]
            public string Mode { get; set; } = "login";

            [Display(Name = "Логин")]
            public string Login { get; set; } = "";

            [Display(Name = "Пароль")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }

            [Display(Name = "Имя")]
            public string FirstName { get; set; } = "";

            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = "";

            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Display(Name = "Телефон")]
            public string Phone { get; set; } = "";

            [Display(Name = "Повтор пароля")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = "";

            [Display(Name = "Принять условия")]
            public bool AcceptTerms { get; set; }
        }
    }
}