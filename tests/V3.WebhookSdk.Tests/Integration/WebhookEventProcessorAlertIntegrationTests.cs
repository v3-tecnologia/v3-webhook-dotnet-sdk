using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Persistence;
using Domain.Events.V1;

namespace V3.WebhookSdk.Tests.Integration
{
    public class WebhookEventProcessorAlertIntegrationTests
    {
        private readonly WebhookEventProcessorBuilder _builder;
        private readonly InMemoryEventWriter _writer = new InMemoryEventWriter();
        private readonly InMemoryEventReader _reader;

        public WebhookEventProcessorAlertIntegrationTests()
        {
            _reader = new InMemoryEventReader(_writer);
            _builder = new WebhookEventProcessorBuilder()
                .WithPersistence(
                   _reader,
                   _writer
                );
        }

        private async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Payloads",
                "events",
                "alert-events",
                fileName
            );

            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        [Theory]
        [InlineData("alert-impact.json", "IMPACT")]
        public async Task Should_process_alert_events(
            string fileName,
            string eventName)
        {
            var eventJson = await ReadPayloadAsync(fileName);

            var webhookWrapper = new
            {
                id = Guid.NewGuid().ToString(),
                created_at = DateTime.UtcNow.ToString("o"),
                attributes = new[] { JsonNode.Parse(eventJson) }
            };

            var payload = JsonSerializer.Serialize(webhookWrapper);

            var processor = _builder
                .OnEvent(
                    EventSelector.Of()
                        .Group("ALERT")
                        .EventName(eventName)
                        .Build(),
                    async (EventContext ctx, ImpactEvent evt) =>
                    {
                        Console.WriteLine("[TEST] Impact Event Handler called!");
                        Console.WriteLine($"[TEST] EventName: {eventName}");
                        Console.WriteLine($"[TEST] PayloadKind: {ctx.PayloadKind}");
                        Console.WriteLine($"[TEST] Context: {JsonSerializer.Serialize(ctx)}");
                        Console.WriteLine($"[TEST] Event: {JsonSerializer.Serialize(evt)}");

                        await ctx.SaveAsync(evt);
                        return EventHandlingResult.Success();
                    }
                )
                .Build();


            var result = await processor.ProcessWebhookAsync(payload);

            if (!result.IsSuccess)
            {
                Console.WriteLine("[TEST][ERROR] Error message: " + result.ErrorMessage);
                if (result.Exception != null)
                {
                    Console.WriteLine("[TEST][ERROR] Exception: " + result.Exception);
                }
                Console.WriteLine("[TEST][ERROR] Payload: " + payload);
            }
            Assert.True(result.IsSuccess, result.ErrorMessage);

            var persisted = await _reader.GetEventsAsync<ImpactEvent>();
            Console.WriteLine($"[TEST] Persisted Impact Events Count: {persisted.Count}");
            Console.WriteLine($"[TEST] Persisted Impact Events: {JsonSerializer.Serialize(persisted)}");

            Assert.Single(persisted);
            Assert.NotNull(persisted[0]);
        }
    }
}