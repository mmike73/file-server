using FileServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileServer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<FileEntry> FileEntries { get; set; }
    public DbSet<FilePermission> FilePermissions { get; set; }
}