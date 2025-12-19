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
    public class WebhookEventProcessorDriverBehaviorIntegrationTests
    {
        private WebhookEventProcessorBuilder _builder;

        public WebhookEventProcessorDriverBehaviorIntegrationTests()
        {
            _builder = new WebhookEventProcessorBuilder();
        }

        private async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Payloads", "events", "driver-behavior-events", fileName);
            Console.WriteLine($"Looking for file: {fileName} in {path}");
            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        [Theory]
        [InlineData("telemetry-harsh-acceleration.json", DriverBehaviorEventNames.HarshAcceleration)]
        [InlineData("telemetry-harsh-braking.json", DriverBehaviorEventNames.HarshBraking)]
        [InlineData("telemetry-max-speed-fault.json", DriverBehaviorEventNames.MaxSpeedExceeded)]
        [InlineData("telemetry-normal-speed-return.json", DriverBehaviorEventNames.NormalSpeedReturn)]
        [InlineData("telemetry-persistent-max-speed.json", DriverBehaviorEventNames.PersistentMaxSpeed)]
        [InlineData("telemetry-sharp-turn.json", DriverBehaviorEventNames.CorneringHarsh)]
        [InlineData("telemetry-start-overtaking.json", DriverBehaviorEventNames.StartOvertaking)]
        public async Task Should_process_driver_behavior_events(string fileName, string eventName)
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
                .OnDriverBehaviorEvent(eventName, async (ctx, evt) =>
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