using System.ComponentModel.DataAnnotations;

namespace BookonnectAPI.DTO;

public sealed class ExternalAuthDTO
{
    [Required]
    [RegularExpression("(?i)^google$", ErrorMessage = "Only Google sign-in is supported.")]
    public string Provider { get; set; } = "google";

    [Required]
    [MaxLength(16384)]
    public string IdToken { get; set; } = string.Empty;
}
