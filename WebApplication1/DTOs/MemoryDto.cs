namespace ChatMemoryApi.DTOs;

public class MemoryDto
{
    public int Id { get; set; }

    public string Summary { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}