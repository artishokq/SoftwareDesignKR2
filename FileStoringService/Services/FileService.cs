using System.Security.Cryptography;
using FileStoringService.Data;
using FileStoringService.Models;
using Microsoft.AspNetCore.Http;

namespace FileStoringService.Services;

public class FileService
{
    private readonly FileContext _db;
    private readonly string      _base = Path.Combine(AppContext.BaseDirectory, "uploads");

    public FileService(FileContext db)
    {
        _db = db;
        Directory.CreateDirectory(_base);
    }

    public async Task<Guid> SaveAsync(IFormFile file)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // простой SHA256 хеш
        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(bytes));

        if (_db.Files.Any(x => x.Hash == hash))
            throw new InvalidOperationException("Duplicate file");

        var id   = Guid.NewGuid();
        var name = $"{id}.txt";
        await File.WriteAllBytesAsync(Path.Combine(_base, name), bytes);

        _db.Files.Add(new FileRecord {
            Id              = id,
            OriginalName    = file.FileName,
            Hash            = hash,
            StorageFileName = name,
            Size            = file.Length,
            UploadedAt      = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return id;
    }

    public Stream GetStream(Guid id)
    {
        var rec = _db.Files.Find(id)
                  ?? throw new FileNotFoundException();
        return File.OpenRead(Path.Combine(_base, rec.StorageFileName));
    }

    public bool TryGetMetadata(Guid id, out FileMetadata meta)
    {
        var rec = _db.Files.Find(id);
        if (rec == null)
        {
            meta = default!;
            return false;
        }

        meta = new FileMetadata(
            rec.Id,
            rec.OriginalName,
            rec.Size,
            rec.UploadedAt
        );
        return true;
    }

}
