namespace FileServer.Entities.Dtos;

public class FileEntryDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string HttpUrl { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public FileVisibility Visibility { get; set; }
    public DateTime DateTimeAdded { get; set; }
}