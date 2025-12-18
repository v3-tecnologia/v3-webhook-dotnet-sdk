using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Events;

namespace V3.WebhookSdk.Tests.Integration
{
    public class WebhookEventProcessorDmsIntegrationTests
    {
        private WebhookEventProcessorBuilder _builder;

        public WebhookEventProcessorDmsIntegrationTests()
        {
            _builder = new WebhookEventProcessorBuilder();
        }

        private async Task<string> ReadPayloadAsync(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Payloads", "events", "dms-events", fileName);
            Console.WriteLine($"Looking for file: {fileName} in {path}");
            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");

            return await File.ReadAllTextAsync(path);
        }
            
        [Theory]
        [InlineData("vision-yawning.json", DmsEventNames.Yawning)]
        [InlineData("vision-drowsiness.json", DmsEventNames.Drowsiness)]
        public async Task Should_process_dms_events(string fileName, string eventName)
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
                .OnDmsEvent(eventName, async (ctx, evt) =>
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