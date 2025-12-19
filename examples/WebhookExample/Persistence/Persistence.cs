using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using V3.WebhookSdk.Handlers;
using WebhookExample.Data;
using Google.Protobuf;
using System.Text.Json;

namespace WebhookExample.Persistence
{
    public class PostgresEventReader : IEventReader
    {
        private readonly WebhookDbContext _db;
        public PostgresEventReader(WebhookDbContext db)
        {
            _db = db;
        }

        public async Task<TEvent?> GetEventByIdAsync<TEvent>(string id) where TEvent : class, IMessage
        {
            var entity = await _db.Events.FindAsync(int.Parse(id));
            if (entity == null) return null;
            var evt = (TEvent)Activator.CreateInstance(typeof(TEvent))!;
            evt.MergeFrom(System.Text.Json.JsonSerializer.Deserialize<byte[]>(entity.Payload));
            return evt;
        }

        public async Task<IReadOnlyList<TEvent>> GetEventsAsync<TEvent>(int max = 10) where TEvent : class, IMessage
        {
            var entities = await _db.Events.OrderByDescending(e => e.ReceivedAt).Take(max).ToListAsync();
            var list = new List<TEvent>();
            foreach (var entity in entities)
            {
                var evt = (TEvent)Activator.CreateInstance(typeof(TEvent))!;
                evt.MergeFrom(System.Text.Json.JsonSerializer.Deserialize<byte[]>(entity.Payload));
                list.Add(evt);
            }
            return list;
        }

        public Task<TEvent?> GetRootEventAsync<TEvent>(TEvent childEvent) where TEvent : class, IMessage
        {
            // Implementação customizada se necessário
            return Task.FromResult<TEvent?>(null);
        }
    }

    public class PostgresEventWriter : IEventWriter
    {
        private readonly WebhookDbContext _db;
        public PostgresEventWriter(WebhookDbContext db)
        {
            _db = db;
        }

        public async Task SaveAsync<TEvent>(EventContext context, TEvent evt) where TEvent : Google.Protobuf.IMessage
        {
            var entity = new WebhookEvent
            {
                EventType = context.Type.ToString(),
                EventGroup = context.PayloadKind.ToString(),
                EventName = context.Status.ToString(),
                Payload = JsonSerializer.Serialize(evt),
                ReceivedAt = DateTime.UtcNow
            };
            _db.Events.Add(entity);
            await _db.SaveChangesAsync();
        }
    }
}
