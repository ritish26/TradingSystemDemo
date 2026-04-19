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

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        // TODO: validate user (hardcoded for now)
        /* if (request.Username != "admin" || request.Password != "password")
            return Unauthorized();*/

        var token = _tokenService.GenerateToken(request.Username);

        return Ok(new AuthResponse { Token = token });
    }
}