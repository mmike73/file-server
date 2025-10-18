using FileServer.Entities;

namespace FileServer.Services;

public interface IFileEntryService
{
    Task<IEnumerable<FileEntry>> GetAllPublicFilesAsync();
    Task<IEnumerable<FileEntry>> GetAllUsersPriateFilesAsync();
    Task<FileEntry?> GetByIdAsync(Guid id);
    Task<FileEntry> CreateAsync(FileEntry fileEntry);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> IsFilePublicAsync(Guid id);
}