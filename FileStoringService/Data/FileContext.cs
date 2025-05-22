using Microsoft.EntityFrameworkCore;
using FileStoringService.Models;

namespace FileStoringService.Data;

public class FileContext : DbContext
{
    public FileContext(DbContextOptions<FileContext> opts) : base(opts) { }
    public DbSet<FileRecord> Files => Set<FileRecord>();
}
