namespace FileStoringService.Models;

public record FileMetadata(
    Guid   FileId,
    string FileName,
    long   Size,
    DateTime UploadedAt
);
