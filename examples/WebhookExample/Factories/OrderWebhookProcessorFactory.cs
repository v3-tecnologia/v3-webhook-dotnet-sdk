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
    public static class OrderWebhookProcessorFactory
    {
        private static readonly EventSelector AckSelector = EventSelector.Of().Group("ORDER").EventName(OrderEventNames.Ack).Build();

        public static WebhookEventProcessor Create(IServiceProvider sp, string webhookSecret)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
            var writer = new PostgresEventWriter(db);
            var reader = new PostgresEventReader(db);
            var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("OrderWebhookProcessor");

            var builder = new WebhookEventProcessorBuilder()
                .WithSignatureValidator(new HmacSha256SignatureValidator(webhookSecret))
                .WithPersistence(reader, writer)
                .OnEvent<OrderStatus>(
                    AckSelector,
                    (EventContext ctx, OrderStatus evt) => EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e))
                );

            return builder.Build();
        }
    }
}
