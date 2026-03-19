using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NewVK.Pages
{
    [Authorize]
    public class NewsModel : PageModel
    {
        public IReadOnlyList<NewsStoryVm> Stories { get; } =
        [
            new()
            {
                Category = "Платформа",
                Title = "Новая главная лента разделена на короткие карточки",
                Summary = "Лента новостей теперь собирает важные обновления продукта, заметки команды и анонсы в одном месте, чтобы пользователю не приходилось искать изменения по разным страницам.",
                PublishedAt = "Сегодня, 09:20",
                Accent = "accent-sand"
            },
            new()
            {
                Category = "Медиа",
                Title = "Загруженные изображения автоматически переходят в WEBP",
                Summary = "Система сохраняет фотографии в более компактном формате и убирает лишние метаданные. Это помогает снизить расход диска и ускоряет отдачу файлов в галерее.",
                PublishedAt = "Сегодня, 10:05",
                Accent = "accent-sage"
            },
            new()
            {
                Category = "Интерфейс",
                Title = "Навигация стала единым боковым модулем",
                Summary = "Страницы профиля, сообщений, фотографий и новостей используют общее меню, поэтому структура сайта читается быстрее и не дублирует действия внутри каждого раздела.",
                PublishedAt = "Вчера, 18:40",
                Accent = "accent-primary"
            },
            new()
            {
                Category = "Профиль",
                Title = "Темы оформления теперь заметнее в ежедневной работе",
                Summary = "Цвета профиля сильнее влияют на фон, карточки и акцентные элементы, поэтому пользователь быстрее понимает, какая тема активна прямо сейчас.",
                PublishedAt = "Вчера, 14:15",
                Accent = "accent-sand"
            }
        ];

        public IReadOnlyList<BriefItemVm> BriefItems { get; } =
        [
            new()
            {
                Label = "Обновлено сегодня",
                Value = "2 раздела",
                Note = "Новости и медиатека получили самостоятельные сценарии."
            },
            new()
            {
                Label = "Экономия места",
                Value = "WEBP",
                Note = "Новые загрузки фотографий проходят оптимизацию при сохранении."
            },
            new()
            {
                Label = "Фокус ленты",
                Value = "Коротко и по делу",
                Note = "Карточки новостей сделаны как обзор изменений без перегруженного интерфейса."
            }
        ];

        public sealed class NewsStoryVm
        {
            public string Category { get; init; } = "";
            public string Title { get; init; } = "";
            public string Summary { get; init; } = "";
            public string PublishedAt { get; init; } = "";
            public string Accent { get; init; } = "";
        }

        public sealed class BriefItemVm
        {
            public string Label { get; init; } = "";
            public string Value { get; init; } = "";
            public string Note { get; init; } = "";
        }
    }
}
