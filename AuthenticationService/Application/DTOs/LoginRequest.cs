using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Application.DTOs;

public class LoginRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}