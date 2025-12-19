using System;
using System.Collections.Generic;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Security;

namespace V3.WebhookSdk.Processing
{
    public sealed class WebhookEventProcessorBuilder
    {
        private readonly Dictionary<(string group, string name), Delegate> _handlers = new();

        private IWebhookSignatureValidator? _signatureValidator;
        private IEventWriter? _eventWriter;
        private IEventReader? _eventReader;

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

        public WebhookEventProcessorBuilder WithEventWriter(IEventWriter writer)
        {
            _eventWriter = writer;
            return this;
        }

        public WebhookEventProcessorBuilder WithEventReader(IEventReader reader)
        {
            _eventReader = reader;
            return this;
        }

        public WebhookEventProcessorBuilder OnEvent<TEvent>(
            EventSelector selector,
            Func<EventContext, TEvent, Task<EventHandlingResult>> handler)
            where TEvent : class
        {
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            _handlers[(selector.Group, selector.EventName)] = handler;
            return this;
        }

        public WebhookEventProcessor Build()
        {
            return new WebhookEventProcessor(
                _handlers,
                _signatureValidator,
                _eventWriter,
                _eventReader
            );
        }
    }
}