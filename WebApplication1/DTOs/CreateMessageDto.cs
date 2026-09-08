using System.ComponentModel.DataAnnotations;

namespace ChatMemoryApi.DTOs;

public class CreateMessageDto
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;
}