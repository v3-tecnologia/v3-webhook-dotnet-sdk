using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using V3.WebhookSdk.Handlers;

namespace V3.WebhookSdk.Persistence
{
    public sealed class InMemoryEventWriter : IEventWriter
    {
        internal readonly Dictionary<string, StoredEvent> Store = new();

        public Task SaveAsync<TEvent>(
            EventContext context,
            TEvent evt)
            where TEvent : IMessage
        {
            Store[context.Id] = new StoredEvent(
                context.Id,
                evt.GetType(),
                evt.ToByteArray()
            );

            return Task.CompletedTask;
        }
    }

    public sealed class InMemoryEventReader : IEventReader
    {
        private readonly Dictionary<string, StoredEvent> _store;

        public InMemoryEventReader(InMemoryEventWriter writer)
        {
            _store = writer.Store;
        }

        public Task<TEvent?> GetEventByIdAsync<TEvent>(string id)
            where TEvent : class, IMessage
        {
            if (!_store.TryGetValue(id, out var stored))
                return Task.FromResult<TEvent?>(null);

            if (stored.Type != typeof(TEvent))
                return Task.FromResult<TEvent?>(null);

            var evt = (TEvent)Activator.CreateInstance(stored.Type)!;
            evt.MergeFrom(stored.Payload);

            return Task.FromResult<TEvent?>(evt);
        }

        public Task<IReadOnlyList<TEvent>> GetEventsAsync<TEvent>(int max = 10)
            where TEvent : class, IMessage
        {
            var events = _store.Values
                .Where(e => e.Type == typeof(TEvent))
                .Take(max)
                .Select(e =>
                {
                    var msg = (TEvent)Activator.CreateInstance(e.Type)!;
                    msg.MergeFrom(e.Payload);
                    return msg;
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<TEvent>>(events);
        }

        public Task<TEvent?> GetRootEventAsync<TEvent>(TEvent childEvent)
            where TEvent : class, IMessage
        {
            var sourceIdProp = childEvent.GetType().GetProperty("SourceId");
            if (sourceIdProp is null)
                return Task.FromResult<TEvent?>(null);

            var sourceId = sourceIdProp.GetValue(childEvent) as string;
            if (string.IsNullOrEmpty(sourceId))
                return Task.FromResult<TEvent?>(null);

            return GetEventByIdAsync<TEvent>(sourceId);
        }
    }

    internal sealed record StoredEvent(
        string Id,
        Type Type,
        byte[] Payload
    );
}