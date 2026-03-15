namespace NewVK.Models
{
    public sealed class UserPhoto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string FileName { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string RelativeUrl { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long SizeBytes { get; set; }

        public DateTime UploadedAtUtc { get; set; }
    }
}