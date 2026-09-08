namespace ChatMemoryApi.DTOs;

public class UserMeDto
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}