using Authentication_Service.Application.DTOs;
using Authentication_Service.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_Service.API.Controllers;

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
    public IActionResult Login(LoginRequest request)
    {
        var user = _authService.Authenticate(request.Username, request.Password);

        if (user is null)
        {
            _logger.LogWarning("Authentication failed for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = _tokenService.GenerateToken(user);
        _logger.LogInformation("User authenticated successfully: {Username}", request.Username);

        return Ok(new AuthResponse { Token = token });
    }
}