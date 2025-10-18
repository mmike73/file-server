namespace FileServer.Entities;

public class FilePermission
{
    public Guid Id { get; set; }
    
    public Guid FileEntryId { get; set; }
    public FileEntry FileEntry { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}