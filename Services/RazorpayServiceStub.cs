namespace ManokshaApi.Services
{
    public class RazorpayServiceStub : IPaymentService
    {
        public Task<(bool ok, object? data)> CreatePaymentOrder(decimal amount, string currency, string receipt)
        {
            var fake = new { id = "order_fake_123", amount = (int)(amount * 100), currency, receipt };
            return Task.FromResult((true, (object)fake));
        }

        public Task<bool> VerifyPaymentSignature(string payload, string signature)
        {
            // In real implementation, verify signature using Razorpay secret
            return Task.FromResult(true);
        }
    }
}
