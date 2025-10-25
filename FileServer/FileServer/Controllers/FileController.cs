using System.Buffers.Text;
using System.Security.Claims;
using FileServer.Entities;
using FileServer.Entities.Dtos;
using FileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileEntryController : ControllerBase
{
    private readonly IFileEntryService _fileService;

    public FileEntryController(IFileEntryService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("all-public")]
    public async Task<IActionResult> GetAllPublic()
    {
        var files = (await _fileService.GetAllPublicFilesAsync())
            .Select(f => new FileEntryDto
            {
                Id = f.Id,
                FileName = f.OriginalName,
                HttpUrl = f.StoredName,
                ContentType = f.ContentType,
                Size = f.Size,
                Visibility = f.Visibility,
                DateTimeAdded = f.DateTimeAdded,
            });
        return Ok(files);
    }
    
    [HttpGet("all-private")]
    [Authorize]
    public async Task<IActionResult> GetAllPrivate()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var files = (await _fileService.GetAllUsersPriateFilesAsync(userId))
            .Select(f => new FileEntryDto
            {
                Id = f.Id,
                FileName = f.OriginalName,
                HttpUrl = f.StoredName,
                ContentType = f.ContentType,
                Size = f.Size,
                Visibility = f.Visibility,
                DateTimeAdded = f.DateTimeAdded,
            });
        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var isAuthenticated = User.Identity?.IsAuthenticated;
        if (isAuthenticated is not null && isAuthenticated == false)
        {
            var publicFile = await _fileService.GetPublicByIdAsync(id);
            if (publicFile == null) return NotFound();
            return Ok(publicFile);
        }
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var privateFile = await _fileService.GetPrivateByIdAsync(id, userId);
        if (privateFile == null) return NotFound();
        return Ok(privateFile);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload([FromForm] FileUploadDto request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var fileEntry = await _fileService.CreateAsync(request, userId);
        return Ok(fileEntry);
    }
    
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var deleted = await _fileService.DeleteAsync(id, userId);
        if (deleted is null) return NotFound();
        return NoContent();
    }
    
[HttpGet("{id}/download")]
public async Task<IActionResult> Download(Guid id)
{ 
    var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
    Guid? userId = null;

    if (isAuthenticated)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var parsedUserId))
            return Unauthorized("Invalid user credentials.");
        
        userId = parsedUserId;
    }

    FileEntry? file = (userId is not null)
        ? await _fileService.GetPrivateByIdAsync(id, userId) 
        : await _fileService.GetPublicByIdAsync(id) ; 
    
    if (file == null)
    {
        return isAuthenticated 
            ? NotFound("File not found or you don't have access to it.")
            : Unauthorized("This file is private. Please log in to access it.");
    }

    try
    {
        var fileStream = await _fileService.DownloadFileAsync(file);
        
        if (fileStream == null)
            return NotFound("File not found on storage server.");

        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = file.OriginalName,
            Inline = false
        };
        Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

        return File(
            fileStream, 
            file.ContentType ?? "application/octet-stream", 
            file.OriginalName,
            enableRangeProcessing: true
        );
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("WebDAV"))
    {
        return StatusCode(503, "Storage server temporarily unavailable. Please try again.");
    }
    catch (Exception ex)
    {
        return StatusCode(500, "An error occurred while downloading the file.");
    }
}

}