namespace V3.WebhookSdk.Processing
{
    public sealed class EventSelector
    {
        public string Group { get; }
        public string EventName { get; }

        private EventSelector(string group, string eventName)
        {
            Group = group;
            EventName = eventName;
        }

        public static Builder Of() => new Builder();

        public sealed class Builder
        {
            private string? _group;
            private string? _eventName;

            public Builder Group(string group)
            {
                _group = group;
                return this;
            }

            public Builder EventName(string eventName)
            {
                _eventName = eventName;
                return this;
            }

            public EventSelector Build()
            {
                if (string.IsNullOrWhiteSpace(_group))
                    throw new InvalidOperationException("EventSelector.Group is required");

                if (string.IsNullOrWhiteSpace(_eventName))
                    throw new InvalidOperationException("EventSelector.EventName is required");

                return new EventSelector(_group!, _eventName!);
            }
        }
    }
}