using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Events;

namespace V3.WebhookSdk.Tests.Integration
{
    public class WebhookEventProcessorAlertIntegrationTests
    {
        private WebhookEventProcessorBuilder _builder;

        public WebhookEventProcessorAlertIntegrationTests()
        {
            _builder = new WebhookEventProcessorBuilder();
        }

        private async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Payloads", "events", "alert-events", fileName);
            Console.WriteLine($"Looking for file: {fileName} in {path}");
            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        [Theory]
        [InlineData("alert-impact.json", "IMPACT")]
        [InlineData("alert-sdcard-mounted.json", "SD_CARD_MOUNTED")]
        [InlineData("alert-sdcard-unmounted.json", "SD_CARD_UNMOUNTED")]
        public async Task Should_process_alert_events(string fileName, string eventName)
        {
            var eventJson = await ReadPayloadAsync(fileName);

            var webhookWrapper = new
            {
                id = Guid.NewGuid().ToString(),
                created_at = DateTime.UtcNow.ToString("o"),
                attributes = new[] { JsonNode.Parse(eventJson) } 
            };

            var payload = JsonSerializer.Serialize(webhookWrapper);

            var handled = false;

            var processor = _builder
                .OnAlertEvent(eventName, async (ctx, evt) =>
                {
                    handled = true;
                    Console.WriteLine("[TEST] Handler called!");
                    Console.WriteLine($"[TEST] ctx: {System.Text.Json.JsonSerializer.Serialize(ctx)}");
                    Console.WriteLine($"[TEST] evt: {System.Text.Json.JsonSerializer.Serialize(evt)}");
                    await Task.CompletedTask;
                })
                .Build();

            Console.WriteLine("[TEST] Payload sent to processor: " + payload);

            await processor.ProcessWebhookAsync(payload);

            Assert.True(handled);
        }
    }
}