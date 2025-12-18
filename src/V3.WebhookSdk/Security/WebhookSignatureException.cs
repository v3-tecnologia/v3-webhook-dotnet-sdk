using System;

namespace V3.WebhookSdk.Security
{
    public class WebhookSignatureException : Exception
    {
        public WebhookSignatureException(string message) : base(message)
        {
        }
    }
}
