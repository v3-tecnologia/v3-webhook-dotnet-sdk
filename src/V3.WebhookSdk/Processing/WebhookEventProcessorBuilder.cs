using System;
using System.Collections.Generic;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Security;

namespace V3.WebhookSdk.Processing
{
    public class WebhookEventProcessorBuilder
    {
        private readonly Dictionary<(string group, string name), Delegate> _handlers = new();
        private IWebhookSignatureValidator? _signatureValidator;

        public WebhookEventProcessorBuilder WithHmacSha256(string secret)
        {
            _signatureValidator = new HmacSha256SignatureValidator(secret);
            return this;
        }

        public WebhookEventProcessorBuilder WithSignatureValidator(
            IWebhookSignatureValidator validator)
        {
            _signatureValidator = validator;
            return this;
        }

        public WebhookEventProcessorBuilder OnDmsEvent(
            string eventName,
            DmsEventHandler handler)
        {
            _handlers[("DMS", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnOrderEvent(
            string eventName,
            OrderEventHandler handler)
        {
            _handlers[("ORDER", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnConnectionEvent(
            string eventName,
            ConnectionEventHandler handler)
        {
            _handlers[("CONNECTION", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnVisionEvent(
            string eventName,
            VisionEventHandler handler)
        {
            _handlers[("VISION", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnHardwareEvent(
            string eventName,
            HardwareEventHandler handler)
        {
            _handlers[("HEALTH", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnSystemEvent(
            string eventName,
            SystemEventHandler handler)
        {
            _handlers[("SYSTEM", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnTelemetryEvent(
            string eventName,
            TelemetryEventHandler handler)
        {
            _handlers[("TELEMETRY", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnAlertEvent(
            string eventName,
            AlertEventHandler handler)
        {
            _handlers[("ALERT", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnDriverBehaviorEvent(
            string eventName,
            DriverBehaviorEventHandler handler)
        {
            _handlers[("DRIVER_BEHAVIOR", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessorBuilder OnVehicleEvent(
            string eventName,
            VehicleEventHandler handler)
        {
            _handlers[("VEHICLE", eventName)] = handler;
            return this;
        }

        public WebhookEventProcessor Build()
        {
            return new WebhookEventProcessor(
                _handlers,
                _signatureValidator
            );
        }
    }
}
