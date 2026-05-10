using AuthenticationService.Application.DTOs;
using AuthenticationService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserAuthenticationService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        IUserAuthenticationService authService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = _authService.Authenticate(request.Username, request.Password);

        if (user is null)
        {
            _logger.LogWarning("Authentication failed for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        _logger.LogInformation("User authenticated successfully: {Username}", request.Username);

        return Ok(new AuthResponse { Token = token });
    }
}