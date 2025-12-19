using Microsoft.Extensions.DependencyInjection;

using Domain.Events.V1;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Persistence;
using V3.WebhookSdk.Security;

using WebhookExample.Data;
using WebhookExample.Persistence;
using WebhookExample.Utils;

namespace WebhookExample.Factories
{
    public static class AlertWebhookProcessorFactory
    {
        private static readonly EventSelector ImpactSelector = EventSelector.Of().Group("ALERT").EventName("IMPACT").Build();

        public static WebhookEventProcessor Create(IServiceProvider sp, string webhookSecret)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
            var writer = new PostgresEventWriter(db);
            var reader = new PostgresEventReader(db);
            var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("AlertWebhookProcessor");

            var builder = new WebhookEventProcessorBuilder()
                .WithSignatureValidator(new HmacSha256SignatureValidator(webhookSecret))
                .WithPersistence(reader, writer)
                .OnEvent<ImpactEvent>(
                    ImpactSelector,
                    (EventContext ctx, ImpactEvent evt) => EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e))
                );

            return builder.Build();
        }
    }
}
