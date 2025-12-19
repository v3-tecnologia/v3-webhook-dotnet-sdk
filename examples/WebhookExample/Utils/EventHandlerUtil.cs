using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using V3.WebhookSdk.Handlers;

namespace WebhookExample.Utils
{
    public static class EventHandlerUtil
    {
        public static async Task<V3.WebhookSdk.Handlers.EventHandlingResult> DispatchAsync<TEvent>(
            ILogger logger,
            EventContext ctx,
            TEvent evt,
            Func<EventContext, TEvent, Task> persistAction)
        {
            var contextJson = JsonSerializer.Serialize(ctx, new JsonSerializerOptions { WriteIndented = true });
            var eventJson = JsonSerializer.Serialize(evt, new JsonSerializerOptions { WriteIndented = true });

            logger.LogInformation($"""
==================== [EVENT RECEIVED] ====================
Type:    {typeof(TEvent).Name}
Kind:    {ctx.PayloadKind}
Context: {contextJson}
Event:   {eventJson}
==========================================================
""");

            await persistAction(ctx, evt);
            return EventHandlingResult.Success();
        }
    }
}
