using FileStoringService.Controllers;
using FileStoringService.Models;
using Microsoft.AspNetCore.Http;

namespace FileStoringService.Services;

public class FileStorageService
{
    private readonly string _basePath = Path.Combine(AppContext.BaseDirectory, "Files");
    private readonly Dictionary<string, Guid> _index = new();
    private readonly Dictionary<Guid, FileMetadata> _metadata = new();
    private readonly FileHashingService _hasher;

    public FileStorageService(FileHashingService hasher)
    {
        _hasher = hasher;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<Guid> SaveAsync(IFormFile file)
    {
        // Читаем содержимое
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // Хеш для проверки дубликата
        var hash = _hasher.ComputeHash(bytes);
        if (_index.ContainsKey(hash))
            throw new InvalidOperationException("Duplicate file");

        // Генерация ID и сохранение на диск
        var id = Guid.NewGuid();
        _index[hash] = id;
        var filePath = Path.Combine(_basePath, $"{id}.txt");
        await File.WriteAllBytesAsync(filePath, bytes);

        // Сохраняем метаданные
        _metadata[id] = new FileMetadata(
            FileId: id,
            FileName: file.FileName,
            Size: file.Length,
            UploadedAt: DateTime.UtcNow
        );

        return id;
    }

    public Stream Get(Guid id)
    {
        var path = Path.Combine(_basePath, $"{id}.txt");
        if (!File.Exists(path))
            throw new FileNotFoundException();
        return File.OpenRead(path);
    }

    /// <summary>
    /// Пытаемся получить метаданные для данного ID.
    /// </summary>
    public bool TryGetMetadata(Guid id, out FileMetadata metadata)
    {
        return _metadata.TryGetValue(id, out metadata!);
    }
}