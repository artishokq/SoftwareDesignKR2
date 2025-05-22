namespace FileStoringService.Services;
using System.Security.Cryptography;

public class FileHashingService
{
    public string ComputeHash(byte[] data)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(data));
    }
}
