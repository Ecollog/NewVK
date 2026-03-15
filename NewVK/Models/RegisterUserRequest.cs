namespace NewVK.Models
{
    public sealed class RegisterUserRequest
    {
        public string Login { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? AboutMe { get; set; }

        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
    }
}