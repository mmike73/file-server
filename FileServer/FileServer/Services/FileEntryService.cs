using FileServer.Data;
using FileServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileServer.Services;

public class FileEntryService(AppDbContext context) : IFileEntryService
{
    public async Task<IEnumerable<FileEntry>> GetAllPublicFilesAsync()
    {
        return await context.FileEntries
            .Include(f => f.Visibility.Equals(FileVisibility.Public))
            .ToListAsync();
    }

    public async Task<IEnumerable<FileEntry>> GetAllUsersPriateFilesAsync()
    {
        return await context.FileEntries
            .Include(f => f.Permissions)
            .ToListAsync();
    }

    public async Task<FileEntry?> GetByIdAsync(Guid id)
    {
        return await context.FileEntries
            .Include(f => f.Permissions)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<FileEntry> CreateAsync(FileEntry fileEntry)
    {
        fileEntry.Id = Guid.NewGuid();
        fileEntry.DateTimeAdded = DateTime.UtcNow;

        context.FileEntries.Add(fileEntry);
        await context.SaveChangesAsync();
        return fileEntry;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var file = await context.FileEntries.FindAsync(id);
        if (file == null) return false;

        context.FileEntries.Remove(file);
        await context.SaveChangesAsync();
        return true;    }

    public async Task<bool> IsFilePublicAsync(Guid id)
    {
        var file = await context.FileEntries.FindAsync(id);
        if (file == null) return false;
        return file.Visibility == FileVisibility.Public;    }
}