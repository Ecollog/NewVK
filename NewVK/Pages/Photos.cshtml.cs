using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using NewVK.Data;
using NewVK.Models;
using NewVK.Services;

namespace NewVK.Pages
{
    [Authorize]
    public class PhotosModel : PageModel
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif"
        };

        private readonly CurrentUserService _currentUserService;
        private readonly UsersRepository _usersRepository;
        private readonly UserPhotosRepository _userPhotosRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly UploadedImageWebpService _uploadedImageWebpService;

        public PhotosModel(
            CurrentUserService currentUserService,
            UsersRepository usersRepository,
            UserPhotosRepository userPhotosRepository,
            IWebHostEnvironment environment,
            UploadedImageWebpService uploadedImageWebpService)
        {
            _currentUserService = currentUserService;
            _usersRepository = usersRepository;
            _userPhotosRepository = userPhotosRepository;
            _environment = environment;
            _uploadedImageWebpService = uploadedImageWebpService;
        }

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public string CurrentUserName { get; private set; } = "";
        public string CurrentUserLogin { get; private set; } = "";

        public IReadOnlyList<PhotoItemVm> Photos { get; private set; } = Array.Empty<PhotoItemVm>();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();
            if (userId is null)
                return RedirectToPage("/Index");

            await LoadPageAsync(userId.Value, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();
            if (userId is null)
                return RedirectToPage("/Index");

            if (UploadFile is null || UploadFile.Length == 0)
            {
                ErrorMessage = "Выберите файл для загрузки.";
                return RedirectToPage();
            }

            if (UploadFile.Length > MaxFileSize)
            {
                ErrorMessage = "Файл слишком большой. Максимум 5 МБ.";
                return RedirectToPage();
            }

            string extension = Path.GetExtension(UploadFile.FileName);
            if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(UploadFile.ContentType))
            {
                ErrorMessage = "Разрешены только изображения: JPG, PNG, WEBP, GIF.";
                return RedirectToPage();
            }

            ProcessedImageFile processedImage;
            try
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "photos");
                processedImage = await _uploadedImageWebpService.ConvertAndSaveAsync(
                    UploadFile,
                    uploadsFolder,
                    "uploads/photos",
                    cancellationToken);
            }
            catch (UnknownImageFormatException)
            {
                ErrorMessage = "Файл не удалось распознать как изображение.";
                return RedirectToPage();
            }
            catch (InvalidImageContentException)
            {
                ErrorMessage = "Изображение повреждено или имеет неподдерживаемый формат.";
                return RedirectToPage();
            }

            var photo = new UserPhoto
            {
                UserId = userId.Value,
                FileName = processedImage.StoredFileName,
                OriginalFileName = Path.GetFileName(UploadFile.FileName),
                RelativeUrl = processedImage.RelativeUrl,
                ContentType = processedImage.ContentType,
                SizeBytes = processedImage.SizeBytes
            };

            await _userPhotosRepository.CreateAsync(photo, cancellationToken);

            SuccessMessage = "Фото загружено и сохранено в формате WEBP.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
        {
            int? userId = _currentUserService.GetUserId();
            if (userId is null)
                return RedirectToPage("/Index");

            UserPhoto? photo = await _userPhotosRepository.GetByIdAsync(id, userId.Value, cancellationToken);
            if (photo is null)
            {
                ErrorMessage = "Фото не найдено.";
                return RedirectToPage();
            }

            string relativePath = photo.RelativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

            bool deleted = await _userPhotosRepository.DeleteAsync(id, userId.Value, cancellationToken);

            if (deleted && System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            SuccessMessage = deleted ? "Фото удалено." : "Не удалось удалить фото.";
            return RedirectToPage();
        }

        private async Task LoadPageAsync(int userId, CancellationToken cancellationToken)
        {
            AppUser? user = await _usersRepository.GetByIdAsync(userId, cancellationToken);
            if (user is not null)
            {
                CurrentUserName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(CurrentUserName))
                    CurrentUserName = user.Login;

                CurrentUserLogin = user.Login;
            }

            IReadOnlyList<UserPhoto> photos = await _userPhotosRepository.GetByUserIdAsync(userId, cancellationToken);

            Photos = photos.Select(x => new PhotoItemVm
            {
                Id = x.Id,
                RelativeUrl = x.RelativeUrl,
                OriginalFileName = x.OriginalFileName,
                UploadedAtText = x.UploadedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                SizeText = FormatSize(x.SizeBytes)
            }).ToList();
        }

        private static string FormatSize(long bytes)
        {
            double size = bytes;
            string[] units = { "Б", "КБ", "МБ", "ГБ" };
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }

        public sealed class PhotoItemVm
        {
            public int Id { get; set; }
            public string RelativeUrl { get; set; } = "";
            public string OriginalFileName { get; set; } = "";
            public string UploadedAtText { get; set; } = "";
            public string SizeText { get; set; } = "";
        }
    }
}
