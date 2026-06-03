namespace WebApplication1.Models
{
    public class Memory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Summary { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
