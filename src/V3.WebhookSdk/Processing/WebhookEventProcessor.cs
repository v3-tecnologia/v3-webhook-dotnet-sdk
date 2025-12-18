using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Security;
using Domain.Notifications.V1;

namespace V3.WebhookSdk.Processing
{
    public class WebhookEventProcessor
    {
        private readonly Dictionary<(string group, string name), Delegate> _handlers;
        private readonly IWebhookSignatureValidator? _signatureValidator;

        public WebhookEventProcessor(
            Dictionary<(string group, string name), Delegate> handlers,
            IWebhookSignatureValidator? signatureValidator = null)
        {
            _handlers = handlers;
            _signatureValidator = signatureValidator;
        }

        public async Task ProcessWebhookAsync(string jsonPayload, string? signature = null)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
                throw new InvalidOperationException("Webhook payload is empty");

            if (_signatureValidator is not null)
            {
                if (string.IsNullOrEmpty(signature))
                    throw new WebhookSignatureException("Missing webhook signature");

                _signatureValidator.Validate(jsonPayload, signature);
            }

            Webhook webhook;
            try
            {
                webhook = Webhook.Parser.ParseJson(jsonPayload);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to parse webhook JSON payload into protobuf",
                    ex
                );
            }

            await ProcessWebhookInternalAsync(webhook);
        }

        private async Task ProcessWebhookInternalAsync(Webhook webhook)
        {
            if (webhook.Attributes is null || webhook.Attributes.Count == 0)
                return;

            foreach (var evt in webhook.Attributes)
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
                        continue;

                    var (group, name, envelope, innerEvent) = resolved.Value;

                    if (!_handlers.TryGetValue((group, name), out var handler))
                        continue;

                    var handlerParams = handler.Method.GetParameters();
                    if (handlerParams.Length != 2)
                        throw new InvalidOperationException("Handler must have 2 parameters");

                    var expectedType = handlerParams[1].ParameterType;

                    var payload =
                        innerEvent != null && expectedType.IsInstanceOfType(innerEvent)
                            ? innerEvent
                            : envelope;

                    var context = new EventContext
                    {
                        Id = evt.Id,
                        Status = evt.Status,
                        CreatedAt = evt.CreatedAt,
                        Type = evt.Type,
                        Category = evt.Category,
                        Sub = evt.Sub,
                        Device = evt.Attributes.Device,
                        Order = evt.Attributes.Order,
                        Location = ExtractLocationRecursive(payload)
                    };

                    var result = handler.DynamicInvoke(context, payload);

                    if (result is not Task task)
                        throw new InvalidOperationException(
                            $"Handler for ({group}, {name}) must return a Task"
                        );

                    await task;
                }

                if (evt.Attributes?.Order is { } orderObj)
                {
                    var group = "ORDER";
                    string name = string.Empty;
                    var statusProp = orderObj.GetType().GetProperty("Status");
                    if (statusProp != null)
                    {
                        var statusValue = statusProp.GetValue(orderObj);
                        if (statusValue != null)
                        {
                            name = $"ORDER_STATUS_{statusValue.ToString().ToUpper()}";
                        }
                    }

                    if (_handlers.TryGetValue((group, name), out var handler))
                    {
                        var context = new EventContext
                        {
                            Id = evt.Id,
                            Status = evt.Status,
                            CreatedAt = evt.CreatedAt,
                            Type = evt.Type,
                            Category = evt.Category,
                            Sub = evt.Sub,
                            Device = evt.Attributes.Device,
                            Order = orderObj,
                            Location = ExtractLocationRecursive(orderObj)
                        };

                        var result = handler.DynamicInvoke(context, orderObj);

                        if (result is not Task task)
                            throw new InvalidOperationException(
                                $"Handler for ({group}, {name}) must return a Task"
                            );

                        await task;
                    }
     
                }

            }
        }

        private static (string group, string name, object envelope, object? innerEvent)? ResolveEvent(object container)
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

            var eventName = GetProperty(envelope, "EventName") as string;
            if (string.IsNullOrEmpty(eventName))
                return null;

            var inner = ExtractOneOf(envelope);

            return (group, eventName, envelope, inner);
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
               .GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
               ?.GetValue(obj);

        private static Domain.Location.V1.Location? ExtractLocationRecursive(object eventObj)
        {
            var type = eventObj.GetType();

            if (type.GetProperty("Location")?.GetValue(eventObj) is Domain.Location.V1.Location loc)
                return loc;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                    continue;

                if (prop.GetValue(eventObj) is not object value)
                    continue;

                var nested = ExtractLocationRecursive(value);
                if (nested is not null)
                    return nested;
            }

            return null;
        }
    }
}
