using Microsoft.EntityFrameworkCore;

namespace WebhookExample.Data
{
    public class WebhookEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventGroup { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }

    public class WebhookDbContext : DbContext
    {
        public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options) { }
        public DbSet<WebhookEvent> Events => Set<WebhookEvent>();
    }
}
