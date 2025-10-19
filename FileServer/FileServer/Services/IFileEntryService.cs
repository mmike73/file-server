using FileServer.Entities;
using FileServer.Entities.Dtos;

namespace FileServer.Services;

public interface IFileEntryService
{
    Task<IEnumerable<FileEntry>> GetAllPublicFilesAsync();
    Task<IEnumerable<FileEntry>> GetAllUsersPriateFilesAsync(Guid userId);
    Task<FileEntry?> GetPublicByIdAsync(Guid id);
    Task<FileEntry?> GetPrivateByIdAsync(Guid id, Guid userId);
    Task<FileEntry> CreateAsync(FileUploadDto fileEntry, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<bool> IsFilePublicAsync(Guid id);
    Task<Stream?> DownloadFileAsync(FileEntry fileEntry);

}