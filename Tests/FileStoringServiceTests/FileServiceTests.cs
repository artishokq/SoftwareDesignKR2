using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileStoringService.Data;
using FileStoringService.Models;
using FileStoringService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.FileStoringServiceTests;

public class FileServiceAdditionalTests
{
    private FileService CreateService(out FileContext db)
    {
        var opts = new DbContextOptionsBuilder<FileContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new FileContext(opts);
        return new FileService(db);
    }

    private IFormFile MakeFile(string content, string name = "file.txt")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var ms = new MemoryStream(bytes);
        return new FormFile(ms, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
    }

    [Fact]
    public async Task SaveAsync_MultipleDifferentFiles_AllStored()
    {
        // Arrange
        var svc = CreateService(out var db);

        var ids = new Guid[3];
        // Act
        ids[0] = await svc.SaveAsync(MakeFile("A"));
        ids[1] = await svc.SaveAsync(MakeFile("B"));
        ids[2] = await svc.SaveAsync(MakeFile("C"));

        // Assert
        Assert.Equal(3, db.Files.Count());
        foreach (var id in ids)
        {
            Assert.True(db.Files.Any(f => f.Id == id));
        }
    }

    [Fact]
    public void TryGetMetadata_TimestampIncreasing_OnSequentialSaves()
    {
        // Arrange
        var svc = CreateService(out var db);
        var id1 = svc.SaveAsync(MakeFile("X")).Result;
        Task.Delay(10).Wait();
        var id2 = svc.SaveAsync(MakeFile("Y")).Result;

        // Act
        svc.TryGetMetadata(id1, out var m1);
        svc.TryGetMetadata(id2, out var m2);

        // Assert
        Assert.True(m2.UploadedAt > m1.UploadedAt);
    }

    [Fact]
    public void GetStream_AfterMultipleSaves_ReturnsCorrectForEach()
    {
        // Arrange
        var svc = CreateService(out _);
        var id1 = svc.SaveAsync(MakeFile("ONE")).Result;
        var id2 = svc.SaveAsync(MakeFile("TWO")).Result;

        // Act & Assert for id1
        using (var r1 = new StreamReader(svc.GetStream(id1)))
        {
            Assert.Equal("ONE", r1.ReadToEnd());
        }

        using (var r2 = new StreamReader(svc.GetStream(id2)))
        {
            Assert.Equal("TWO", r2.ReadToEnd());
        }
    }

    [Fact]
    public void SaveAsync_EmptyFile_AllowsZeroLength()
    {
        // Arrange
        var svc = CreateService(out var db);
        var empty = new byte[0];
        var file = new FormFile(new MemoryStream(empty), 0, 0, "file", "empty.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var id = svc.SaveAsync(file).Result;

        // Assert
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(0, db.Files.Single(f => f.Id == id).Size);
        using var reader = new StreamReader(svc.GetStream(id));
        Assert.Equal("", reader.ReadToEnd());
    }
}