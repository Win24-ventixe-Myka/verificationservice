using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class SendVerificationCodeRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}