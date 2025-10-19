namespace FileServer.Entities;

public class FileEntry
{
    public Guid Id { get; set; }
    
    public string OriginalName { get; set; }
    
    public string StoredName { get; set; }

    public string Path { get; set; } = null!;

    public string HttpUrl { get; set; } = "";

    public string Extension { get; set; } = "";
    public long Size { get; set; }
    
    public DateTime DateTimeAdded { get; set; }

    public FileVisibility Visibility { get; set; } = FileVisibility.Private;
    
    public Guid AddedBy { get; set; }
    public User? AddedByUser { get; set; } = null!;
    
    public string? ContentType { get; set; }
    public string? CheckSum { get; set; }

    public ICollection<FilePermission> Permissions { get; set; } = new List<FilePermission>();
}