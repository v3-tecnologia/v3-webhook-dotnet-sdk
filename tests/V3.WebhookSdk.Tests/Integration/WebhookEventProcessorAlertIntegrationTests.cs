using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using Domain.Events.V1;

namespace V3.WebhookSdk.Tests.Integration
{
    public class WebhookEventProcessorAlertIntegrationTests
    {
        private readonly WebhookEventProcessorBuilder _builder;

        public WebhookEventProcessorAlertIntegrationTests()
        {
            _builder = new WebhookEventProcessorBuilder();
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

                        await Task.CompletedTask;
                        return EventHandlingResult.Success();
                    }
                )
                .Build();

            var result = await processor.ProcessWebhookAsync(payload);

            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

    }
}