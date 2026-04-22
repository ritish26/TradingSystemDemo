using Authentication_Service.Model.Request;
using Authentication_Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_Service.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Login endpoint for user authentication. Validates user credentials and returns a JWT token if successful.
    /// Write now we are giving token for two users: user and admin 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        // TODO: hardcoded user validation for demonstration purposes
        var token = _tokenService.GenerateToken(request.Username);
        
        return Ok(new AuthResponse { Token = token });
    }
}