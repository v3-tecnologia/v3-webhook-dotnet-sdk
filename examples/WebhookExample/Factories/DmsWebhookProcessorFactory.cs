using Microsoft.Extensions.DependencyInjection;

using Domain.Events.V1;
using V3.WebhookSdk.Events;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;

using WebhookExample.Data;
using WebhookExample.Persistence;
using WebhookExample.Utils;

namespace WebhookExample.Factories
{
    public static class DmsWebhookProcessorFactory
    {
        private static readonly EventSelector YawningSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Yawning).Build();
        private static readonly EventSelector DrowsinessSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drowsiness).Build();
        private static readonly EventSelector DrinkingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drinking).Build();
        private static readonly EventSelector EatingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Eating).Build();
        private static readonly EventSelector EyeClosureSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.EyeClosure).Build();
        private static readonly EventSelector GazeDistractionSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeDistraction).Build();
        private static readonly EventSelector GazeFixationSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeFixation).Build();
        private static readonly EventSelector PoseDistractionPitchSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionPitch).Build();
        private static readonly EventSelector PoseDistractionYawSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionYaw).Build();
        private static readonly EventSelector SmokingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Smoking).Build();
        private static readonly EventSelector OnPhoneSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.OnPhone).Build();

        public static WebhookEventProcessor Create(IServiceProvider sp, string webhookSecret)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
            var writer = new PostgresEventWriter(db);
            var reader = new PostgresEventReader(db);
            var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DmsWebhookProcessor");

            var builder = new WebhookEventProcessorBuilder()
                .WithSignatureValidator(new HmacSha256SignatureValidator(webhookSecret))
                .WithPersistence(reader, writer)
                .OnEvent<YawningEvent>(YawningSelector, (EventContext ctx, YawningEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<DrowsinessEvent>(DrowsinessSelector, (EventContext ctx, DrowsinessEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<DrinkingEvent>(DrinkingSelector, (EventContext ctx, DrinkingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<EatingEvent>(EatingSelector, (EventContext ctx, EatingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<EyeClosureEvent>(EyeClosureSelector, (EventContext ctx, EyeClosureEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<GazeDistractionEvent>(GazeDistractionSelector, (EventContext ctx, GazeDistractionEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<GazeFixationEvent>(GazeFixationSelector, (EventContext ctx, GazeFixationEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<PoseDistractionPitchEvent>(PoseDistractionPitchSelector, (EventContext ctx, PoseDistractionPitchEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<PoseDistractionYawEvent>(PoseDistractionYawSelector, (EventContext ctx, PoseDistractionYawEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<SmokingEvent>(SmokingSelector, (EventContext ctx, SmokingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<OnPhoneEvent>(OnPhoneSelector, (EventContext ctx, OnPhoneEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)));

            return builder.Build();
        }
    }
}
