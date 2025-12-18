namespace V3.WebhookSdk.Security
{
    public interface IWebhookSignatureValidator
    {
        void Validate(string payload, string signature);
    }
}
