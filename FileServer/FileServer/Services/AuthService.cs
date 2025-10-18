using System.Security.Claims;
using System.Text;
using FileServer.Data;
using FileServer.Entities;
using FileServer.Entities.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace FileServer.Services;

public class AuthService(AppDbContext context, IConfiguration configuration) : IAuthService
{

    private String CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        
        var key = new SymmetricSecurityKey (
            Encoding.UTF8.GetBytes(configuration["AppSettings:Token"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: configuration["AppSettings:Issuer"],
            audience: configuration["AppSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await context.SaveChangesAsync();
        return refreshToken;
    }

    private async Task<User?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.RefreshToken.Equals(refreshToken));
        if (user is null)
        {
            return null;
        }
        
        if (user.RefreshToken != refreshToken 
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return null;
        }

        return user;
    }
    
    private async Task<TokenResponseDto> CreateTokenResponse(User? user)
    {
        return new TokenResponseDto
        {
            AccessToken = CreateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
        };
    }


    
    public async Task<User?> RegisterAsync(UserDto request)
    {
        // TODO: add all sorts of checks - tolower, blah blah
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return null;
        }

        var user = new User();
        var hashedPassword = new PasswordHasher<User>()
            .HashPassword(user, request.Password);

        user.Username = request.Username;
        user.PasswordHash = hashedPassword;
        
        user.Email = "";
            
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<TokenResponseDto?> LoginAsync(UserDto request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null)
        {
            return null;
        }
        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return await CreateTokenResponse(user);
    }

    public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var user = await ValidateRefreshTokenAsync(request.RefreshToken);
        if (user is null)
        {
            return null;
        }

        return await CreateTokenResponse(user);
    }
    
}