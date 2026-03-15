using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewVK.Data;
using NewVK.Models;
using NewVK.Services;

namespace NewVK.Pages
{
    [Authorize]
    public class MessagesModel : PageModel
    {
        private readonly CurrentUserService _currentUserService;
        private readonly UsersRepository _usersRepository;

        public MessagesModel(
            CurrentUserService currentUserService,
            UsersRepository usersRepository)
        {
            _currentUserService = currentUserService;
            _usersRepository = usersRepository;
        }

        [BindProperty(SupportsGet = true)]
        public string? Chat { get; set; }

        public string CurrentUserName { get; private set; } = "";
        public string CurrentUserLogin { get; private set; } = "";
        public string ActiveChatId { get; private set; } = "";

        public IReadOnlyList<DialogVm> Dialogs { get; private set; } = Array.Empty<DialogVm>();
        public DialogVm? ActiveDialog { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();
            if (userId is null)
                return RedirectToPage("/Index");

            AppUser? user = await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user is null)
                return RedirectToPage("/Index");

            CurrentUserName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(CurrentUserName))
                CurrentUserName = user.Login;

            CurrentUserLogin = user.Login;

            Dialogs = BuildDemoDialogs(CurrentUserName);

            ActiveDialog = Dialogs.FirstOrDefault(x => x.Id == Chat) ?? Dialogs.FirstOrDefault();
            ActiveChatId = ActiveDialog?.Id ?? "";

            return Page();
        }

        private static IReadOnlyList<DialogVm> BuildDemoDialogs(string currentUserName)
        {
            return new List<DialogVm>
            {
                new()
                {
                    Id = "anna",
                    UserName = "Анна Власова",
                    UserHandle = "@anna.v",
                    Preview = "Я посмотрела фото, получилось очень хорошо.",
                    LastTime = "12:48",
                    UnreadCount = 2,
                    Messages =
                    {
                        new MessageVm { IsMine = false, Text = "Привет! Ты уже загрузил новые фото?", Time = "12:31" },
                        new MessageVm { IsMine = true,  Text = "Да, сегодня добавил несколько штук.", Time = "12:36" },
                        new MessageVm { IsMine = false, Text = "Я посмотрела фото, получилось очень хорошо.", Time = "12:48" }
                    }
                },
                new()
                {
                    Id = "max",
                    UserName = "Максим Орлов",
                    UserHandle = "@max.orlov",
                    Preview = "Завтра напишу по странице сообщений.",
                    LastTime = "вчера",
                    UnreadCount = 0,
                    Messages =
                    {
                        new MessageVm { IsMine = false, Text = "Я набросал идеи для диалогов и списка друзей.", Time = "18:10" },
                        new MessageVm { IsMine = true,  Text = "Отлично, потом подключим это к БД.", Time = "18:16" },
                        new MessageVm { IsMine = false, Text = "Завтра напишу по странице сообщений.", Time = "18:18" }
                    }
                },
                new()
                {
                    Id = "irina",
                    UserName = "Ирина Белова",
                    UserHandle = "@irina.b",
                    Preview = "Не забудь добавить кнопку перехода в профиль.",
                    LastTime = "пт",
                    UnreadCount = 1,
                    Messages =
                    {
                        new MessageVm { IsMine = false, Text = "Страница фото уже выглядит хорошо.", Time = "17:01" },
                        new MessageVm { IsMine = true,  Text = "Осталось только добавить переходы между разделами.", Time = "17:03" },
                        new MessageVm { IsMine = false, Text = "Не забудь добавить кнопку перехода в профиль.", Time = "17:05" }
                    }
                }
            };
        }

        public sealed class DialogVm
        {
            public string Id { get; set; } = "";
            public string UserName { get; set; } = "";
            public string UserHandle { get; set; } = "";
            public string Preview { get; set; } = "";
            public string LastTime { get; set; } = "";
            public int UnreadCount { get; set; }
            public List<MessageVm> Messages { get; set; } = new();
        }

        public sealed class MessageVm
        {
            public bool IsMine { get; set; }
            public string Text { get; set; } = "";
            public string Time { get; set; } = "";
        }
    }
}