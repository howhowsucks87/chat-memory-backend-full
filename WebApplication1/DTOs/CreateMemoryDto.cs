using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs;

public class CreateMemoryDto
{
    [Required]
    [MaxLength(2000)]
    public string Summary { get; set; } = null!;
}