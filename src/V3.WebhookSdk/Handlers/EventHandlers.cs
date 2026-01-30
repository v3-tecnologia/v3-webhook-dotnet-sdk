using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Domain.Events.V1;
using Domain.Orders.V1;
using Domain.Location.V1;

using OrderEventStatus = Domain.Events.V1.OrderStatus;

namespace V3.WebhookSdk.Handlers
{
    public enum EventPayloadKind
    {
        Dms,
        Order,
        Connection,
        Vision,
        Hardware,
        System,
        Telemetry,
        Alert,
        DriverBehavior,
        Vehicle
    }

    /// <summary>
    /// Responsible for persisting protobuf events.
    /// Implemented by the SDK consumer.
    /// </summary>
    public interface IEventWriter
    {
        Task SaveAsync<TEvent>(
            EventContext context,
            TEvent evt)
            where TEvent : IMessage;
    }

    /// <summary>
    /// Responsible for reading persisted protobuf events and their relationships.
    /// Implemented by the SDK consumer.
    /// </summary>
    public interface IEventReader
    {
        Task<TEvent?> GetEventByIdAsync<TEvent>(string id)
            where TEvent : class, IMessage;

        Task<IReadOnlyList<TEvent>> GetEventsAsync<TEvent>(int max = 10)
            where TEvent : class, IMessage;

        Task<TEvent?> GetRootEventAsync<TEvent>(TEvent childEvent)
            where TEvent : class, IMessage;
    }

    /// <summary>
    /// Rich execution context passed to every event handler.
    /// Provides metadata and persistence helpers.
    /// </summary>
    public sealed class EventContext
    {
        private readonly IEventWriter? _writer;
        private readonly IEventReader? _reader;

        public string Id { get; }
        public bool HasMedia { get; }
        public EventPayloadKind PayloadKind { get; }
        public Status Status { get; }
        public Timestamp CreatedAt { get; }
        public EventType Type { get; }
        public EventCategory Category { get; }
        public EventSub Sub { get; }

        public Device? Device { get; }
        public Location? Location { get; }

        internal EventContext(
            string id,
            bool hasMedia,
            Status status,
            Timestamp createdAt,
            EventType type,
            EventCategory category,
            EventSub sub,
            Device? device,
            Location? location,
            EventPayloadKind payloadKind,
            IEventWriter? writer,
            IEventReader? reader)
        {
            Id = id;
            HasMedia = hasMedia;
            Status = status;
            CreatedAt = createdAt;
            Type = type;
            Category = category;
            Sub = sub;
            Device = device;
            Location = location;
            PayloadKind = payloadKind;
            _writer = writer;
            _reader = reader;
        }

        /// <summary>
        /// Persists the given protobuf event.
        /// </summary>
        public Task SaveAsync<TEvent>(TEvent evt)
            where TEvent : IMessage
        {
            if (_writer is null)
                throw new InvalidOperationException("No IEventWriter configured.");

            return _writer.SaveAsync(this, evt);
        }

        public Task<TEvent?> GetEventByIdAsync<TEvent>(string id)
            where TEvent : class, IMessage
        {
            if (_reader is null)
                throw new InvalidOperationException("No IEventReader configured.");

            return _reader.GetEventByIdAsync<TEvent>(id);
        }

        public Task<IReadOnlyList<TEvent>> GetEventsAsync<TEvent>(int max = 10)
            where TEvent : class, IMessage
        {
            if (_reader is null)
                throw new InvalidOperationException("No IEventReader configured.");

            return _reader.GetEventsAsync<TEvent>(max);
        }

        public Task<TEvent?> GetRootEventAsync<TEvent>(TEvent evt)
            where TEvent : class, IMessage
        {
            if (_reader is null)
                throw new InvalidOperationException("No IEventReader configured.");

            return _reader.GetRootEventAsync(evt);
        }
    }

    /// <summary>
    /// Represents the result of a webhook event handler execution.
    /// </summary>
    public sealed class EventHandlingResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private EventHandlingResult(
            bool success,
            string? errorMessage,
            Exception? exception)
        {
            IsSuccess = success;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static EventHandlingResult Success()
            => new(true, null, null);

        public static EventHandlingResult Failure(
            string errorMessage,
            Exception? exception = null)
            => new(false, errorMessage, exception);
    }
}
