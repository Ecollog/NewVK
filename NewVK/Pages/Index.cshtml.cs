using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private const string UserCookieName = "demo_registered_user";
    private const string AuthCookieName = "demo_auth_user";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string SuccessMessage { get; set; } = "";

    [BindProperty]
    public AuthVm Auth { get; set; } = new() { Mode = "login" };

    public IActionResult OnGet()
    {
        if (IsAuthorized())
            return RedirectToPage("/Profile");

        Auth.Mode = NormalizeMode(Auth.Mode);
        return Page();
    }

    public IActionResult OnPostSubmit()
    {
        Auth.Mode = NormalizeMode(Auth.Mode);

        if (Auth.Mode == "login")
            ClearRegisterValues();

        ValidateForm();

        if (!ModelState.IsValid)
            return Page();

        return Auth.Mode == "register"
            ? HandleRegister()
            : HandleLogin();
    }

    private IActionResult HandleRegister()
    {
        var user = new StoredUser
        {
            Login = Auth.Login.Trim(),
            PasswordHash = HashPassword(Auth.Password),
            FirstName = Auth.FirstName.Trim(),
            LastName = Auth.LastName.Trim(),
            Email = Auth.Email.Trim(),
            Phone = Auth.Phone.Trim()
        };

        WriteUserCookie(user);
        WriteAuthCookie(user.Login, rememberMe: true);

        return RedirectToPage("/Profile");
    }

    private IActionResult HandleLogin()
    {
        var user = ReadUserCookie();

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Сначала зарегистрируйтесь. Сейчас пользователь хранится только в cookie этого браузера.");
            Auth.Mode = "register";
            return Page();
        }

        var loginOrEmail = Auth.Login.Trim();
        var loginMatches = string.Equals(user.Login, loginOrEmail, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(user.Email, loginOrEmail, StringComparison.OrdinalIgnoreCase);

        if (!loginMatches)
        {
            ModelState.AddModelError("Auth.Login", "Пользователь с таким логином или email не найден.");
            return Page();
        }

        var passwordHash = HashPassword(Auth.Password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            ModelState.AddModelError("Auth.Password", "Неверный пароль.");
            return Page();
        }

        WriteAuthCookie(user.Login, Auth.RememberMe);
        return RedirectToPage("/Profile");
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

        var passOk = Regex.IsMatch(
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

    private bool IsAuthorized()
        => Request.Cookies.ContainsKey(AuthCookieName);

    private void WriteUserCookie(StoredUser user)
    {
        var json = JsonSerializer.Serialize(user, JsonOptions);
        Response.Cookies.Append(
            UserCookieName,
            json,
            CreateCookieOptions(DateTimeOffset.UtcNow.AddDays(30)));
    }

    private StoredUser? ReadUserCookie()
    {
        if (!Request.Cookies.TryGetValue(UserCookieName, out var json) || string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<StoredUser>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void WriteAuthCookie(string login, bool rememberMe)
    {
        var expires = rememberMe
            ? DateTimeOffset.UtcNow.AddDays(30)
            : (DateTimeOffset?)null;

        Response.Cookies.Append(AuthCookieName, login, CreateCookieOptions(expires));
    }

    private CookieOptions CreateCookieOptions(DateTimeOffset? expires)
        => new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = expires,
            Path = "/"
        };

    private static string NormalizeMode(string? mode)
        => mode == "register" ? "register" : "login";

    private static string HashPassword(string? password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

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

    private sealed class StoredUser
    {
        public string Login { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}