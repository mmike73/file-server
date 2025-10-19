namespace FileServer.Entities.Dtos;

public class FileUploadDto
{
    public IFormFile File { get; set; }
    public string FileName { get; set; }
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public string Visibility { get; set; }
}