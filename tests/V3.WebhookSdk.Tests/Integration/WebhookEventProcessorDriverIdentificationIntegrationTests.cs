using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Domain.Driver.V1;
using Domain.Events.V1;
using V3.WebhookSdk.Events;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Persistence;
using V3.WebhookSdk.Processing;
using Xunit;

namespace V3.WebhookSdk.Tests.Integration
{
    public class WebhookEventProcessorDriverIdentificationIntegrationTests
    {
        private readonly InMemoryEventWriter _writer = new();
        private readonly InMemoryEventReader _reader;

        public WebhookEventProcessorDriverIdentificationIntegrationTests()
        {
            _reader = new InMemoryEventReader(_writer);
        }

        private static async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Payloads",
                "events",
                "driver-identification-events",
                fileName
            );

            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        private static string WrapWebhook(string eventJson)
        {
            var webhookWrapper = new
            {
                id = Guid.NewGuid().ToString(),
                created_at = DateTime.UtcNow.ToString("o"),
                attributes = new[] { JsonNode.Parse(eventJson) }
            };

            return JsonSerializer.Serialize(webhookWrapper);
        }

        [Fact]
        public async Task Should_process_driver_identification_event()
        {
            var eventJson = await ReadPayloadAsync("driver-identification.json");
            var payload = WrapWebhook(eventJson);
            DriverIdentifiedEvent? received = null;
            EventPayloadKind? payloadKind = null;

            var processor = new WebhookEventProcessorBuilder()
                .WithPersistence(_reader, _writer)
                .OnEvent(
                    EventSelector.Of()
                        .Group(DriverIdentificationEventGroups.Identified)
                        .EventName(DriverIdentificationEventNames.Identified)
                        .Build(),
                    async (EventContext ctx, DriverIdentifiedEvent evt) =>
                    {
                        received = evt;
                        payloadKind = ctx.PayloadKind;
                        await ctx.SaveAsync(evt);
                        return EventHandlingResult.Success();
                    })
                .Build();

            var result = await processor.ProcessWebhookAsync(payload);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(received);
            Assert.Equal(EventPayloadKind.DriverIdentification, payloadKind);
            Assert.Equal("RFID-ABC-123", received!.CredentialIdentifier);
            Assert.NotNull(received.Match);
            Assert.Equal(MatchResult.Matched, received.Match.Result);
            Assert.Equal("driver-001", received.Match.DriverId);

            var persisted = await _reader.GetEventsAsync<DriverIdentifiedEvent>();
            Assert.Single(persisted);
            Assert.Equal("driver-001", persisted[0].Match.DriverId);
        }

        [Fact]
        public async Task Should_process_driver_unidentification_event()
        {
            var eventJson = await ReadPayloadAsync("driver-unidentification.json");
            var payload = WrapWebhook(eventJson);
            DriverUnidentifiedEvent? received = null;
            EventPayloadKind? payloadKind = null;

            var processor = new WebhookEventProcessorBuilder()
                .WithPersistence(_reader, _writer)
                .OnEvent(
                    EventSelector.Of()
                        .Group(DriverIdentificationEventGroups.Unidentified)
                        .EventName(DriverIdentificationEventNames.Unidentified)
                        .Build(),
                    async (EventContext ctx, DriverUnidentifiedEvent evt) =>
                    {
                        received = evt;
                        payloadKind = ctx.PayloadKind;
                        await ctx.SaveAsync(evt);
                        return EventHandlingResult.Success();
                    })
                .Build();

            var result = await processor.ProcessWebhookAsync(payload);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(received);
            Assert.Equal(EventPayloadKind.DriverIdentification, payloadKind);
            Assert.NotNull(received!.Cause);
            Assert.NotNull(received.Cause.Timeout);
            Assert.Equal(TimeoutSource.CameraVirtual, received.Cause.Timeout.TimeoutSource);

            var persisted = await _reader.GetEventsAsync<DriverUnidentifiedEvent>();
            Assert.Single(persisted);
            Assert.Equal(TimeoutSource.CameraVirtual, persisted[0].Cause.Timeout.TimeoutSource);
        }
    }
}
