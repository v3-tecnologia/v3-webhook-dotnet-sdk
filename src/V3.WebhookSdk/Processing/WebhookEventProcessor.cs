using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using Domain.Events.V1;
using Domain.Notifications.V1;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Security;

namespace V3.WebhookSdk.Processing
{
    public sealed class WebhookEventProcessor
    {
        private readonly Dictionary<(string group, string name), Delegate> _handlers;
        private readonly IWebhookSignatureValidator? _signatureValidator;
        private readonly IEventWriter? _eventWriter;
        private readonly IEventReader? _eventReader;

        public WebhookEventProcessor(
            Dictionary<(string group, string name), Delegate> handlers,
            IWebhookSignatureValidator? signatureValidator,
            IEventWriter? eventWriter,
            IEventReader? eventReader)
        {
            _handlers = handlers;
            _signatureValidator = signatureValidator;
            _eventWriter = eventWriter;
            _eventReader = eventReader;
        }

        public async Task<EventHandlingResult> ProcessWebhookAsync(
            string jsonPayload,
            string? signature = null)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                return EventHandlingResult.Failure("Webhook payload is empty");

            if (_signatureValidator is not null)
            {
                if (string.IsNullOrEmpty(signature))
                    return EventHandlingResult.Failure("Missing webhook signature");

                try
                {
                    _signatureValidator.Validate(jsonPayload, signature);
                }
                catch (Exception ex)
                {
                    return EventHandlingResult.Failure(
                        "Invalid webhook signature",
                        ex
                    );
                }
            }

            Webhook webhook;
            try
            {
                webhook = Webhook.Parser.ParseJson(jsonPayload);
            }
            catch (Exception ex)
            {
                return EventHandlingResult.Failure(
                    "Failed to parse webhook JSON payload",
                    ex
                );
            }

            if (webhook.Attributes is null || webhook.Attributes.Count == 0)
                return EventHandlingResult.Success();

            foreach (var evt in webhook.Attributes)
            {
                var result = await ProcessAttributeAsync(evt);
                if (!result.IsSuccess)
                    return result;
            }

            return EventHandlingResult.Success();
        }

        private async Task<EventHandlingResult> ProcessAttributeAsync(Event evt)
        {
            if (evt.Attributes?.Data is { } data)
            {
                var resolved =
                    data.TripEvent is not null
                        ? ResolveEvent(data.TripEvent)
                        : data.StandaloneEvent is not null
                            ? ResolveEvent(data.StandaloneEvent)
                            : null;

                if (resolved is null)
                    return EventHandlingResult.Success();

                var (group, name, payload) = resolved.Value;

                if (!_handlers.TryGetValue((group, name), out var handler))
                    return EventHandlingResult.Success();

                return await InvokeHandlerAsync(handler, evt, payload, group);
            }

            if (evt.Attributes?.Order is { } order)
            {
                var statusProp = order.GetType().GetProperty("Status");
                if (statusProp?.GetValue(order) is not Enum status)
                    return EventHandlingResult.Success();

                var group = "ORDER";
                var name = $"ORDER_STATUS_{status.ToString().ToUpper()}";

                if (!_handlers.TryGetValue((group, name), out var handler))
                    return EventHandlingResult.Success();

                return await InvokeHandlerAsync(handler, evt, order, group);
            }

            return EventHandlingResult.Success();
        }

        private async Task<EventHandlingResult> InvokeHandlerAsync(
            Delegate handler,
            Event evt,
            object payload,
            string group)
        {
            var parameters = handler.Method.GetParameters();
            if (parameters.Length != 2)
                return EventHandlingResult.Failure(
                    "Handler must have exactly 2 parameters"
                );

            var expectedType = parameters[1].ParameterType;
            if (!expectedType.IsInstanceOfType(payload))
                return EventHandlingResult.Failure(
                    $"Handler expects {expectedType.Name}, got {payload.GetType().Name}"
                );

            var context = new EventContext(
                id: evt.Id,
                hasMedia: evt.HasMedia,
                status: evt.Status,
                createdAt: evt.CreatedAt,
                type: evt.Type,
                category: evt.Category,
                sub: evt.Sub,
                device: evt.Attributes?.Device,
                location: ExtractLocationRecursive(payload),
                payloadKind: ResolvePayloadKind(group),
                writer: _eventWriter,
                reader: _eventReader
            );

            try
            {
                var result = handler.DynamicInvoke(context, payload);

                if (result is not Task<EventHandlingResult> task)
                    return EventHandlingResult.Failure(
                        "Handler must return Task<EventHandlingResult>"
                    );

                return await task;
            }
            catch (Exception ex)
            {
                return EventHandlingResult.Failure(
                    $"Handler execution failed for event id={evt.Id}",
                    ex
                );
            }
        }

        private static (string group, string name, object payload)? ResolveEvent(object container)
        {
            var group = GetProperty(container, "EventGroupName") as string;
            if (string.IsNullOrEmpty(group))
                return null;

            object? envelope =
                GetProperty(container, "Adas")
                ?? GetProperty(container, "Alert")
                ?? GetProperty(container, "Connection")
                ?? GetProperty(container, "Dms")
                ?? GetProperty(container, "DriverBehavior")
                ?? GetProperty(container, "Health")
                ?? GetProperty(container, "Vehicle")
                ?? GetProperty(container, "Vision")
                ?? GetProperty(container, "System")
                ?? GetProperty(container, "Telemetry");

            if (envelope is null)
                return null;

            var name = GetProperty(envelope, "EventName") as string;
            if (string.IsNullOrEmpty(name))
                return null;

            var inner = ExtractOneOf(envelope);
            if (inner is null)
                return null;

            return (group, name, inner);
        }

        private static object? ExtractOneOf(object envelope)
        {
            if (envelope is not IMessage msg)
                return null;

            foreach (var oneof in msg.Descriptor.Oneofs)
            {
                foreach (var field in oneof.Fields)
                {
                    var value = field.Accessor.GetValue(msg);
                    if (value is not null)
                        return value;
                }
            }

            return null;
        }

        private static object? GetProperty(object obj, string name) =>
            obj.GetType()
               .GetProperty(
                   name,
                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
               )
               ?.GetValue(obj);

        private static Domain.Location.V1.Location? ExtractLocationRecursive(object evt)
        {
            var type = evt.GetType();

            if (type.GetProperty("Location")?.GetValue(evt)
                is Domain.Location.V1.Location loc)
                return loc;

            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType.IsPrimitive ||
                    prop.PropertyType == typeof(string))
                    continue;

                if (prop.GetValue(evt) is not object value)
                    continue;

                var nested = ExtractLocationRecursive(value);
                if (nested is not null)
                    return nested;
            }

            return null;
        }

        private static EventPayloadKind ResolvePayloadKind(string group)
        {
            return group.ToUpperInvariant() switch
            {
                "DMS" => EventPayloadKind.Dms,
                "ALERT" => EventPayloadKind.Alert,
                "VISION" => EventPayloadKind.Vision,
                "CONNECTION" => EventPayloadKind.Connection,
                "HEALTH" => EventPayloadKind.Hardware,
                "TELEMETRY" => EventPayloadKind.Telemetry,
                "DRIVER_BEHAVIOR" => EventPayloadKind.DriverBehavior,
                "VEHICLE" => EventPayloadKind.Vehicle,
                "SYSTEM" => EventPayloadKind.System,
                "ORDER" => EventPayloadKind.Order,
                _ => EventPayloadKind.System
            };
        }
    }
}