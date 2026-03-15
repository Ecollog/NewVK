namespace NewVK.Models
{
    public sealed class UpdateUserProfileRequest
    {
        public int Id { get; set; }

        public string Login { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? AboutMe { get; set; }

        public string ThemeKey { get; set; } = SiteThemeDefaults.DefaultKey;
    }
}