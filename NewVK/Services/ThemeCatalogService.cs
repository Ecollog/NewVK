using System.Text.Json;
using NewVK.Models;

namespace NewVK.Services
{
    public sealed class ThemeCatalogService
    {
        private readonly Lazy<IReadOnlyList<SiteTheme>> _themes;

        public ThemeCatalogService(IWebHostEnvironment environment)
        {
            string filePath = Path.Combine(environment.WebRootPath, "data", "themes.json");

            _themes = new Lazy<IReadOnlyList<SiteTheme>>(
                () => LoadThemes(filePath),
                isThreadSafe: true);
        }

        public IReadOnlyList<SiteTheme> GetAll()
            => _themes.Value;

        public bool Exists(string? key)
            => GetAll().Any(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

        public SiteTheme GetByKeyOrDefault(string? key)
        {
            IReadOnlyList<SiteTheme> themes = GetAll();

            SiteTheme? found = themes.FirstOrDefault(x =>
                string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

            return found
                   ?? themes.FirstOrDefault(x => x.Key == SiteThemeDefaults.DefaultKey)
                   ?? themes.First();
        }

        private static IReadOnlyList<SiteTheme> LoadThemes(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл тем не найден: {filePath}");

            string json = File.ReadAllText(filePath);

            List<SiteTheme>? themes = JsonSerializer.Deserialize<List<SiteTheme>>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (themes is null || themes.Count == 0)
                throw new InvalidOperationException("В themes.json нет ни одной темы.");

            return themes;
        }
    }
}