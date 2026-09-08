using System.ComponentModel.DataAnnotations;

namespace ChatMemoryApi.DTOs;

public class CreateMemoryDto
{
    [Required]
    [MaxLength(2000)]
    public string Summary { get; set; } = null!;
}