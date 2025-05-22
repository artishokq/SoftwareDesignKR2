namespace FileStoringService.Models;

public class FileRecord
{
    public Guid   Id              { get; set; }
    public string OriginalName    { get; set; } = null!;
    public string Hash            { get; set; } = null!;
    public string StorageFileName { get; set; } = null!;
    public long   Size            { get; set; }
    public DateTime UploadedAt    { get; set; }
}
