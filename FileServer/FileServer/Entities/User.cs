namespace FileServer.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public DateTime Created { get; set; }

    public string? Role { get; set; }

    public ICollection<FileEntry> Files { get; set; } = new List<FileEntry>();
}