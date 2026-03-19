using System.Globalization;
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
        private static readonly CultureInfo RussianCulture = new("ru-RU");

        private readonly CurrentUserService _currentUserService;
        private readonly UsersRepository _usersRepository;
        private readonly MessagesRepository _messagesRepository;

        public MessagesModel(
            CurrentUserService currentUserService,
            UsersRepository usersRepository,
            MessagesRepository messagesRepository)
        {
            _currentUserService = currentUserService;
            _usersRepository = usersRepository;
            _messagesRepository = messagesRepository;
        }

        [BindProperty(SupportsGet = true)]
        public int? Chat { get; set; }

        [BindProperty]
        public SendMessageInput Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public string CurrentUserName { get; private set; } = "";
        public string CurrentUserLogin { get; private set; } = "";
        public int ActiveChatId { get; private set; }

        public IReadOnlyList<DialogVm> Dialogs { get; private set; } = Array.Empty<DialogVm>();
        public DialogVm? ActiveDialog { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            AppUser? user = await GetCurrentUserAsync(cancellationToken);
            if (user is null)
                return RedirectToPage("/Index");

            await LoadPageAsync(user, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostSendAsync(CancellationToken cancellationToken)
        {
            AppUser? user = await GetCurrentUserAsync(cancellationToken);
            if (user is null)
                return RedirectToPage("/Index");

            ValidateInput();

            if (!ModelState.IsValid)
            {
                await LoadPageAsync(user, cancellationToken);
                return Page();
            }

            SendDirectMessageResult result = await _messagesRepository.SendDirectMessageByLoginAsync(
                user.Id,
                Input.TargetLogin,
                Input.Text,
                cancellationToken);

            if (result.Status == SendDirectMessageStatus.RecipientNotFound)
            {
                ModelState.AddModelError("Input.TargetLogin", "Пользователь с таким логином не найден.");
                await LoadPageAsync(user, cancellationToken);
                return Page();
            }

            if (result.Status == SendDirectMessageStatus.CannotMessageSelf)
            {
                ModelState.AddModelError("Input.TargetLogin", "Нельзя отправить сообщение самому себе.");
                await LoadPageAsync(user, cancellationToken);
                return Page();
            }

            SuccessMessage = "Сообщение отправлено.";
            return RedirectToPage("/Messages", new { chat = result.ConversationId });
        }

        private async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();
            if (userId is null)
                return null;

            return await _usersRepository.GetByIdAsync(userId.Value, cancellationToken);
        }

        private async Task LoadPageAsync(AppUser user, CancellationToken cancellationToken)
        {
            CurrentUserName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(CurrentUserName))
                CurrentUserName = user.Login;

            CurrentUserLogin = user.Login;

            if (Chat.HasValue)
                await _messagesRepository.MarkAsReadAsync(Chat.Value, user.Id, cancellationToken);

            List<DialogVm> dialogs = (await _messagesRepository.GetDialogsAsync(user.Id, cancellationToken))
                .Select(MapDialog)
                .ToList();

            Dialogs = dialogs;

            ActiveDialog = dialogs.FirstOrDefault(x => x.Id == Chat) ?? dialogs.FirstOrDefault();
            ActiveChatId = ActiveDialog?.Id ?? 0;

            if (ActiveDialog is not null)
            {
                ActiveDialog.Messages = (await _messagesRepository.GetMessagesAsync(ActiveDialog.Id, user.Id, cancellationToken))
                    .Select(message => new MessageVm
                    {
                        IsMine = message.SenderUserId == user.Id,
                        Text = message.Body,
                        Time = FormatMessageTime(message.SentAtUtc)
                    })
                    .ToList();

                if (string.IsNullOrWhiteSpace(Input.TargetLogin))
                    Input.TargetLogin = ActiveDialog.UserLogin;
            }
        }

        private void ValidateInput()
        {
            Input.TargetLogin = (Input.TargetLogin ?? string.Empty).Trim();
            Input.Text = (Input.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(Input.TargetLogin))
                ModelState.AddModelError("Input.TargetLogin", "Введите логин получателя.");
            else if (Input.TargetLogin.Length > 50)
                ModelState.AddModelError("Input.TargetLogin", "Логин слишком длинный.");

            if (string.IsNullOrWhiteSpace(Input.Text))
                ModelState.AddModelError("Input.Text", "Введите текст сообщения.");
            else if (Input.Text.Length > 4000)
                ModelState.AddModelError("Input.Text", "Сообщение не должно превышать 4000 символов.");
        }

        private static DialogVm MapDialog(MessageDialogListItem dialog)
        {
            return new DialogVm
            {
                Id = dialog.ConversationId,
                UserName = dialog.OtherUserName,
                UserLogin = dialog.OtherUserLogin,
                UserHandle = "@"+dialog.OtherUserLogin,
                Preview = BuildPreview(dialog.Preview),
                LastTime = FormatDialogTime(dialog.LastMessageAtUtc),
                UnreadCount = dialog.UnreadCount
            };
        }

        private static string BuildPreview(string value)
        {
            string compact = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (compact.Length <= 90)
                return compact;

            return compact[..87].TrimEnd() + "...";
        }

        private static string FormatDialogTime(DateTime utc)
        {
            DateTime local = ToLocalTime(utc);
            DateTime today = ToLocalTime(DateTime.UtcNow).Date;

            if (local.Date == today)
                return local.ToString("HH:mm", RussianCulture);

            if (local.Date == today.AddDays(-1))
                return "вчера";

            if (local.Date >= today.AddDays(-6))
                return RussianCulture.DateTimeFormat.GetAbbreviatedDayName(local.DayOfWeek).TrimEnd('.');

            return local.ToString("dd.MM", RussianCulture);
        }

        private static string FormatMessageTime(DateTime utc)
        {
            DateTime local = ToLocalTime(utc);
            DateTime today = ToLocalTime(DateTime.UtcNow).Date;

            return local.Date == today
                ? local.ToString("HH:mm", RussianCulture)
                : local.ToString("dd.MM HH:mm", RussianCulture);
        }

        private static DateTime ToLocalTime(DateTime utc)
        {
            DateTime utcValue = utc.Kind == DateTimeKind.Utc
                ? utc
                : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcValue, TimeZoneInfo.Local);
        }

        public sealed class SendMessageInput
        {
            public string TargetLogin { get; set; } = "";
            public string Text { get; set; } = "";
        }

        public sealed class DialogVm
        {
            public int Id { get; set; }
            public string UserName { get; set; } = "";
            public string UserLogin { get; set; } = "";
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
