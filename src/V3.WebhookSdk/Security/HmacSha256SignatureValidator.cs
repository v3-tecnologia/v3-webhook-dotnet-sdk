using System;
using System.Security.Cryptography;
using System.Text;

namespace V3.WebhookSdk.Security
{
    public sealed class HmacSha256SignatureValidator : IWebhookSignatureValidator
    {
        private readonly byte[] _secret;

        public HmacSha256SignatureValidator(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("HMAC secret cannot be null or empty");

            _secret = Encoding.UTF8.GetBytes(secret);
        }

        public void Validate(string payload, string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                throw new WebhookSignatureException("Missing webhook signature");

            using var hmac = new HMACSHA256(_secret);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computed),
                    Encoding.UTF8.GetBytes(signature.ToLowerInvariant())))
            {
                throw new WebhookSignatureException("Invalid webhook signature");
            }
        }
    }
}
