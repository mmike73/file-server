using FileServer.Data;
using FileServer.Entities;
using FileServer.Entities.Dtos;
using Microsoft.EntityFrameworkCore;
using WebDav;

namespace FileServer.Services;

public class FileEntryService(AppDbContext context, IWebDavClient webDavClient) : IFileEntryService
{
    public async Task<IEnumerable<FileEntry>> GetAllPublicFilesAsync()
    {
        return await context.FileEntries
            // .Include(f => f.Visibility)
            .Where(f => f.Visibility.Equals(FileVisibility.Public))
            .ToListAsync();
    }

    public async Task<IEnumerable<FileEntry>> GetAllUsersPriateFilesAsync(Guid userId)
    {
        return await context.FileEntries
            .Where(f => f.AddedBy.Equals(userId) && f.Visibility.Equals(FileVisibility.Private))
            .Include(f => f.Permissions)
            .ToListAsync();
    }

    public async Task<FileEntry?> GetPublicByIdAsync(Guid id)
    {
        var file = await context.FileEntries.FirstOrDefaultAsync(f => f.Id == id);
        
        if (file == null) return null;
        if (file.Visibility.Equals(FileVisibility.Public)) return file;
        
        return null;
    }

    public async Task<FileEntry?> GetPrivateByIdAsync(Guid id, Guid userId)
    {
        var file = await context.FileEntries.FirstOrDefaultAsync(f => f.Id == id);
        
        if (file == null) return null;
        if (file.Visibility.Equals(FileVisibility.Public)) return file;
        if (file.Visibility.Equals(FileVisibility.Private) && file.AddedBy.Equals(userId)) return file;
        
        return null;
    }

    public async Task<FileEntry> CreateAsync(FileUploadDto fileDto, Guid userId)
    {
        var file = fileDto.File;
        var storedName = $"{Guid.NewGuid()}";
        
        var fileEntry = new FileEntry
        {
            Id = Guid.NewGuid(),
            OriginalName = file.FileName,
            StoredName = storedName,
            Extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "",
            Path = $"/{storedName}",
            Size = file.Length,
            AddedBy = userId,
            Visibility = ("Private".Equals(fileDto.Visibility)  ? FileVisibility.Private : FileVisibility.Public),
            ContentType = file.ContentType,
            DateTimeAdded = DateTime.UtcNow,
        };
        
        await using var stream = file.OpenReadStream();
        var result = await webDavClient.PutFile($"/webdav/{storedName}", stream, file.ContentType);
        
        if (!result.IsSuccessful)
            throw new Exception($"WebDAV upload failed: {result.StatusCode} {result.Description}");
        
        fileEntry.HttpUrl = $"{fileEntry.Path}".Replace("//", "/");
        
        context.FileEntries.Add(fileEntry);
        await context.SaveChangesAsync();
        
        return fileEntry;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var file = await context.FileEntries.FindAsync(id);
        // TODO - throw apropriate errors
        if (file == null)
        {
            return false;
        }

        if (!file.AddedBy.Equals(userId))
        {
            return false;
        }
        
        var propfindResult = await webDavClient.Propfind(file.Path);
        if (!propfindResult.IsSuccessful)
        {
            context.FileEntries.Remove(file);
            await context.SaveChangesAsync();
            return true;
        }
        
        var deleteResult = await webDavClient.Delete(file.Path);
        if (!deleteResult.IsSuccessful)
            throw new Exception($"WebDAV delete failed: {deleteResult.StatusCode}");
        
        context.FileEntries.Remove(file);
        await context.SaveChangesAsync();
        return true;    
    }

    public async Task<bool> IsFilePublicAsync(Guid id)
    {
        var file = await context.FileEntries.FindAsync(id);
        if (file == null) return false;
        return file.Visibility == FileVisibility.Public;    
    }

    public async Task<Stream?> DownloadFileAsync(FileEntry fileEntry)
    {
        try
        {
            var response = await webDavClient.GetRawFile(fileEntry.StoredName);
            if (!response.IsSuccessful)
                return null;

            // Return stream so ASP.NET Core can stream directly to the HTTP response
            return response.Stream;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download failed: {ex.Message}");
            return null;
        }
    }
}