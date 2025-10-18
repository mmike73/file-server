using FileServer.Entities;
using FileServer.Entities.Dtos;
using FileServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.Controllers;

[Microsoft.AspNetCore.Components.Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserDto request)
    {
        var user = await authService.RegisterAsync(request);

        if (user is null)
            return BadRequest("Username already exists.");

        return Ok(user);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(UserDto request)
    {
        var result = await authService.LoginAsync(request);

        if (result is null)
            return BadRequest("Invalid username or password.");

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var result = await authService.RefreshTokenAsync(request);
        if (result is null || result.AccessToken is null || result.RefreshToken is null)
        {
            return Unauthorized("Invalid refresh token.");
        }

        return Ok(result);
    }
    
}