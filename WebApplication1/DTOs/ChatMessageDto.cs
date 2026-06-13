namespace WebApplication1.DTOs;

public class ChatMessageDto
{
    public int Id { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}