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
        var files = await _fileService.GetAllPublicFilesAsync();
        return Ok(files);
    }
    
    [HttpGet("all-private")]
    [Authorize]
    public async Task<IActionResult> GetAllPrivate()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var files = await _fileService.GetAllUsersPriateFilesAsync(userId);
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
        if (!deleted) return NotFound();
        return NoContent();
    }
    
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    { var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        FileEntry? file;
        if (!isAuthenticated)
        {
            file = await _fileService.GetPublicByIdAsync(id);
            if (file == null)
                return Unauthorized("This file is private. Please log in to access it.");
        }
        else
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            file = await _fileService.GetPrivateByIdAsync(id, userId);
            if (file == null)
                return NotFound("File not found or access denied.");
        }

        var fileStream = await _fileService.DownloadFileAsync(file);
        if (fileStream == null)
            return NotFound("File not found on WebDAV server.");

        return File(fileStream, file.ContentType ?? "application/octet-stream", file.OriginalName);
    }

}