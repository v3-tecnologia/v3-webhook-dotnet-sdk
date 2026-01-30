using V3.WebhookSdk.Events;
using Domain.Events.V1;
using V3.WebhookSdk.Handlers;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using V3.WebhookSdk.Processing;

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
            if (!File.Exists(path))
                throw new FileNotFoundException($"The payload file was not found: {path}");
            return await File.ReadAllTextAsync(path);
        }

        [Theory]
        [InlineData("vision-yawning.json", "YAWNING")]
        [InlineData("vision-drowsiness.json", "DROWSINESS")]
        [InlineData("vision-drinking.json", "DRINKING")]
        [InlineData("vision-eating.json", "EATING")]
        [InlineData("vision-eye-closure.json", "EYE_CLOSURE")]
        [InlineData("vision-gaze-distraction.json", "GAZE_DISTRACTION")]
        [InlineData("vision-gaze-fixation.json", "GAZE_FIXATION")]
        [InlineData("vision-pose-distraction-pitch.json", "POSE_DISTRACTION_PITCH")]
        [InlineData("vision-pose-distraction-yaw.json", "POSE_DISTRACTION_YAW")]
        [InlineData("vision-smoking.json", "SMOKING")]
        [InlineData("vision-on-phone.json", "ON_PHONE")]
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
            var builder = new WebhookEventProcessorBuilder()
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.Yawning).Build(), async (EventContext ctx, YawningEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drowsiness).Build(), async (EventContext ctx, DrowsinessEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drinking).Build(), async (EventContext ctx, DrinkingEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.Eating).Build(), async (EventContext ctx, EatingEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.EyeClosure).Build(), async (EventContext ctx, EyeClosureEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeDistraction).Build(), async (EventContext ctx, GazeDistractionEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeFixation).Build(), async (EventContext ctx, GazeFixationEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionPitch).Build(), async (EventContext ctx, PoseDistractionPitchEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionYaw).Build(), async (EventContext ctx, PoseDistractionYawEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.Smoking).Build(), async (EventContext ctx, SmokingEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .OnEvent(EventSelector.Of().Group("DMS").EventName(DmsEventNames.OnPhone).Build(), async (EventContext ctx, OnPhoneEvent evt) => { handled = true; await Task.CompletedTask; return EventHandlingResult.Success(); })
                .Build();
            var processor = builder;
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
            Assert.True(handled, "Handler was not called");
        }
    }
}
