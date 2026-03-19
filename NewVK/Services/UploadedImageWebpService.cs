using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace NewVK.Services
{
    public sealed class UploadedImageWebpService
    {
        private static readonly WebpEncoder Encoder = new()
        {
            Quality = 76,
            Method = WebpEncodingMethod.BestQuality
        };

        public async Task<ProcessedImageFile> ConvertAndSaveAsync(
            IFormFile file,
            string targetDirectory,
            string targetRelativeDirectory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetRelativeDirectory);

            Directory.CreateDirectory(targetDirectory);

            string storedFileName = $"{Guid.NewGuid():N}.webp";
            string physicalPath = Path.Combine(targetDirectory, storedFileName);

            await using var inputStream = file.OpenReadStream();
            using Image image = await Image.LoadAsync(inputStream, cancellationToken);

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            await using (var outputStream = System.IO.File.Create(physicalPath))
            {
                await image.SaveAsWebpAsync(outputStream, Encoder, cancellationToken);
            }

            var fileInfo = new FileInfo(physicalPath);

            return new ProcessedImageFile
            {
                StoredFileName = storedFileName,
                RelativeUrl = CombineRelativeUrl(targetRelativeDirectory, storedFileName),
                ContentType = "image/webp",
                SizeBytes = fileInfo.Length
            };
        }

        private static string CombineRelativeUrl(string relativeDirectory, string fileName)
        {
            string normalizedDirectory = relativeDirectory.Trim().Trim('/').Replace('\\', '/');
            return $"/{normalizedDirectory}/{fileName}";
        }
    }

    public sealed class ProcessedImageFile
    {
        public string StoredFileName { get; init; } = "";
        public string RelativeUrl { get; init; } = "";
        public string ContentType { get; init; } = "";
        public long SizeBytes { get; init; }
    }
}
