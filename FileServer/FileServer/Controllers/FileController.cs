using FileServer.Entities;
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
        // TODO - umpack the token and determine the user
        var files = await _fileService.GetAllUsersPriateFilesAsync();
        return Ok(files);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var file = await _fileService.GetByIdAsync(id);
        if (file == null) return NotFound();
        return Ok(file);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] FileEntry fileEntry)
    {
        var created = await _fileService.CreateAsync(fileEntry);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _fileService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("access/{id}")]
    public async Task<IActionResult> AccessFile(Guid id)
    {
        var file = await _fileService.GetByIdAsync(id);
        if (file == null) return NotFound();

        if (file.Visibility == FileVisibility.Private)
        {
            // TODO - require token
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized("This file is private. You need to log in.");

            // TODO - check permissions
        }

        return Ok(new
        {
            file.Id,
            file.OriginalName,
            file.StoredName,
            file.Path,
            file.ContentType,
            file.Size,
            file.Visibility
        });
    }
}