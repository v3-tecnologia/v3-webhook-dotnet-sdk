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
    public class WebhookEventProcessorTelemetryIntegrationTests
    {
        private WebhookEventProcessorBuilder _builder;

        public WebhookEventProcessorTelemetryIntegrationTests()
        {
            _builder = new WebhookEventProcessorBuilder();
        }

        private async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Payloads", "events", "telemetry-events", fileName);
            Console.WriteLine($"Looking for file: {fileName} in {path}");
            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        [Theory]
        [InlineData("telemetry-ignition.json", "IGNITION")]
        [InlineData("telemetry-device-battery.json", "BATTERY")]
        [InlineData("telemetry-periodic.json", "PERIODIC")]
        [InlineData("telemetry-vehicle-battery.json", "BATTERY")]
        public async Task Should_process_telemetry_events(string fileName, string eventName)
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
                .OnTelemetryEvent(eventName, async (ctx, evt) =>
                {
                    handled = true;
                    Console.WriteLine("[TEST] Handler chamado!");
                    Console.WriteLine($"[TEST] ctx: {System.Text.Json.JsonSerializer.Serialize(ctx)}");
                    Console.WriteLine($"[TEST] evt: {System.Text.Json.JsonSerializer.Serialize(evt)}");
                    await Task.CompletedTask;
                })
                .Build();

            Console.WriteLine("[TEST] Payload enviado para processor:");
            Console.WriteLine(payload);

            await processor.ProcessWebhookAsync(payload);

            Assert.True(handled);
        }
    }
}