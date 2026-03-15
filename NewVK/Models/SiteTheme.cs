namespace NewVK.Models
{
    public sealed class SiteTheme
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Primary { get; set; } = "";
        public string Sand { get; set; } = "";
        public string Sage { get; set; } = "";
        public string Bg { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public static class SiteThemeDefaults
    {
        public const string DefaultKey = "earthy";
    }
}